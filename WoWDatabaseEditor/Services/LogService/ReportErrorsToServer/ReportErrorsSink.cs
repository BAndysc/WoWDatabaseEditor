using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog.Core;
using Serilog.Events;
using WDE.Common;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Common.Utils;

namespace WoWDatabaseEditorCore.Services.LogService.ReportErrorsToServer;

public class ReportErrorsSink : ILogEventSink
{
    private HttpClient? client;
    private bool sinkClosed;
    private string? serverUri;

    private ConcurrentQueue<LogEvent> queue = new();

    public ReportErrorsSink()
    {
    }

    public void OpenSink(IApplicationReleaseConfiguration config, IHttpClientFactory factory)
    {
        serverUri = config.GetString("UPDATE_SERVER");
        if (string.IsNullOrEmpty(serverUri))
        {
            CloseSink();
            return;
        }

        client = factory.Factory();
        if (queue.TryDequeue(out var @event))
        {
            ReportError(client, @event).ListenErrors();
        }
    }

    public void CloseSink()
    {
        LOG.LogInformation("Closed error reporting sink.");
        sinkClosed = true;
        queue.Clear();
    }

    public void Emit(LogEvent logEvent)
    {
        if (sinkClosed)
            return;

        if (logEvent.Level >= LogEventLevel.Error)
        {
            if (TryGetClient() is { } client)
                ReportError(client, logEvent).ListenErrors();
            else
                queue.Enqueue(logEvent);
        }
    }

    private async Task ReportError(HttpClient client, LogEvent logEvent)
    {
        while (queue.TryDequeue(out var log))
            await UploadLog(client, log);

        await UploadLog(client, logEvent);
    }

    private HttpClient? TryGetClient()
    {
        return client;
    }

    private async Task UploadLog(HttpClient client, LogEvent logEvent)
    {
        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[{logEvent.Timestamp:hh:mm:ss} {logEvent.Level}] {logEvent.RenderMessage()}");
        if ((logEvent.Properties?.Count ?? 0) > 0)
        {
            logBuilder.AppendLine($"Template: " + logEvent.MessageTemplate.Text);
            foreach (var property in logEvent.Properties!)
                logBuilder.AppendLine($"   {property.Key}: {property.Value}");
        }
        if (logEvent.Exception != null)
            logBuilder.AppendLine(logEvent.Exception.ToString());

        var request = new
        {
            log = logBuilder.ToString()
        };

        await client.PostAsync(Path.Join(serverUri, "Log", "Send"), new StringContent(JsonConvert.SerializeObject(request), new MediaTypeHeaderValue("application/json")));
    }
}