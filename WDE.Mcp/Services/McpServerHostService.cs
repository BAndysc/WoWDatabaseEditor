using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Mcp.Settings;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WDE.Mcp.Services;

[AutoRegister]
[SingleInstance]
public class McpServerHostService : IGlobalService, IMcpServerStatus, IDisposable
{
    private readonly IMcpSettingsProvider settings;
    private readonly IMcpToolRegistry registry;
    private readonly IMainThread mainThread;
    private readonly McpJsonRpcDispatcher dispatcher;
    private readonly CancellationTokenSource cts = new();
    private HttpListener? listener;

    private bool isRunning;
    private string? url;
    private string? lastError;
    private int toolCount;

    public McpServerHostService(IMcpSettingsProvider settings,
        IMcpToolRegistry registry,
        IMainThread mainThread,
        ILoadingEventAggregator loadingEventAggregator)
    {
        this.settings = settings;
        this.registry = registry;
        this.mainThread = mainThread;
        dispatcher = new McpJsonRpcDispatcher(registry);
        loadingEventAggregator.OnEvent<EditorLoaded>().SubscribeOnce(_ => Start());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsRunning => isRunning;
    public string? Url => url;
    public string? LastError => lastError;
    public int ToolCount => toolCount;

    private void Start()
    {
        if (!settings.Current.Enabled)
            return;

        var port = settings.Current.Port;
        var prefix = $"http://127.0.0.1:{port}/";
        try
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[MCP] Could not start MCP server at {prefix}: {e.Message}");
            listener = null;
            UpdateStatus(false, null, e.Message);
            return;
        }

        Task.Run(() => AcceptLoop(cts.Token));
        Console.WriteLine($"[MCP] MCP server listening at {prefix}");
        UpdateStatus(true, prefix, null);
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        var currentListener = listener;
        if (currentListener == null)
            return;

        // materializing the tool catalog off the hot path (and off the UI thread ctors is fine:
        // tools only store service references in their constructors)
        var count = registry.ToolCount;
        UpdateStatus(true, url, null, count);

        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await currentListener.GetContextAsync();
            }
            catch (Exception) when (token.IsCancellationRequested || !currentListener.IsListening)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleRequest(context, token);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[MCP] Request handling failed: {e}");
                    TryClose(context, 500);
                }
            }, token);
        }
    }

    private async Task HandleRequest(HttpListenerContext context, CancellationToken token)
    {
        var request = context.Request;
        var response = context.Response;

        // DNS rebinding protection: we are loopback only, reject cross-origin browser calls
        var origin = request.Headers["Origin"];
        if (origin != null && Uri.TryCreate(origin, UriKind.Absolute, out var originUri) &&
            !originUri.IsLoopback)
        {
            TryClose(context, 403);
            return;
        }

        if (request.HttpMethod != "POST")
        {
            response.AddHeader("Allow", "POST");
            TryClose(context, 405);
            return;
        }

        string body;
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            body = await reader.ReadToEndAsync(token);

        JsonRpcMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<JsonRpcMessage>(body, McpJsonUtilities.DefaultOptions);
        }
        catch (JsonException e)
        {
            await WriteJson(response, 400, ParseError(e.Message));
            return;
        }

        if (message == null)
        {
            await WriteJson(response, 400, ParseError("Empty request"));
            return;
        }

        var result = await dispatcher.Handle(message, token);
        if (result == null)
        {
            // notification - accepted, no content
            TryClose(context, 202);
            return;
        }

        await WriteJson(response, 200, JsonSerializer.Serialize(result, McpJsonUtilities.DefaultOptions));
    }

    private static string ParseError(string message)
    {
        var error = new JsonRpcError
        {
            Error = new JsonRpcErrorDetail
            {
                Code = (int)McpErrorCode.ParseError,
                Message = message
            }
        };
        return JsonSerializer.Serialize(error, McpJsonUtilities.DefaultOptions);
    }

    private static async Task WriteJson(HttpListenerResponse response, int statusCode, string json)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
            response.Close();
        }
        catch (Exception)
        {
            // client disconnected mid-response
        }
    }

    private static void TryClose(HttpListenerContext context, int statusCode)
    {
        try
        {
            context.Response.StatusCode = statusCode;
            context.Response.Close();
        }
        catch (Exception)
        {
            // already closed
        }
    }

    private void UpdateStatus(bool running, string? newUrl, string? error, int? tools = null)
    {
        mainThread.Dispatch(() =>
        {
            isRunning = running;
            url = newUrl;
            lastError = error;
            if (tools.HasValue)
                toolCount = tools.Value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Url)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastError)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolCount)));
        });
    }

    public void Dispose()
    {
        cts.Cancel();
        try
        {
            listener?.Stop();
            listener?.Close();
        }
        catch (Exception)
        {
            // shutting down anyway
        }
        listener = null;
    }
}
