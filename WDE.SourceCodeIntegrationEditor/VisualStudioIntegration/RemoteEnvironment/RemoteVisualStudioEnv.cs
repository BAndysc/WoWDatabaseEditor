using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using EnvDTE;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Debugging;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.RemoteEnvironment;

internal class RemoteVisualStudioEnv : IDTE
{
    private readonly IMainThread mainThread;
    private readonly string host;
    private readonly int port;
    private readonly string key;
    private Socket? socket;
    private bool disposed;
    private const int timeOut = 2000;

    private List<TaskCompletionSource<RemoteVisualStudioPacket>> pendingRequests = new();

    public RemoteVisualStudioEnv(IMainThread mainThread, string host, int port, string key)
    {
        this.mainThread = mainThread;
        this.host = host;
        this.port = port;
        this.key = key;
    }

    public async Task Listen()
    {
        byte[] headerBuffer = new byte[RemoteVisualStudioHeader.BufferLength];
        byte[] buffer = new byte[1024];

        while (!disposed)
        {
            try
            {
                var length = await socket!.ReceiveAsync(headerBuffer);
                if (length != headerBuffer.Length)
                    throw new Exception("Invalid header length " + length);

                var header = new RemoteVisualStudioHeader(headerBuffer);

                if (header.BytesLength >= ushort.MaxValue)
                    throw new Exception("Packet too big!");

                if (buffer.Length < header.BytesLength)
                    buffer = new byte[header.BytesLength];

                int remainingSize = header.BytesLength;
                while (remainingSize > 0)
                {
                    var read = await socket.ReceiveAsync(buffer.AsMemory(header.BytesLength  - remainingSize, remainingSize));
                    if (read == 0)
                        throw new Exception("Connection closed");
                    remainingSize -= read;
                }

                var packet = new RemoteVisualStudioPacket(header, buffer.AsSpan(0, header.BytesLength));

                LOG.LogInformation($"Received: [{packet.Header.Type}] {packet.Message} (len={packet.Header.BytesLength})");

                if (header.Type == RemoteVisualStudioPacketType.Execute ||
                    header.Type == RemoteVisualStudioPacketType.AuthOk)
                {
                    TaskCompletionSource<RemoteVisualStudioPacket>? tcs = null;
                    lock (this)
                    {
                        if (pendingRequests.Count > 0)
                        {
                            tcs = pendingRequests[0];
                            pendingRequests.RemoveAt(0);
                        }
                    }
                    tcs?.SetResult(packet);
                }
                else if (header.Type == RemoteVisualStudioPacketType.Reverse)
                {
                    ProcessReverseCommand(packet);
                }
                else if (header.Type == RemoteVisualStudioPacketType.FatalError)
                {
                    await DisposeAsync();
                    lock (this)
                    {
                        pendingRequests.ForEach(x => x.SetException(new Exception("Fatal error: " + packet.Message)));
                        pendingRequests.Clear();
                    }
                    throw new Exception("Fatal error: " + packet.Message);
                }
                else
                {
                    LOG.LogError("Unknown packet type: " + header.Type);
                }
            }
            catch (Exception e)
            {
                LOG.LogError(e, message: "Listening thread encountered an exception");
                break;
            }
        }
    }

    private void ProcessReverseCommand(RemoteVisualStudioPacket packet)
    {
        var message = packet.Message;

        if (!message.StartsWith("Event"))
            return;

        message = message.Substring(5);

        if (message.StartsWith("RunModeEntered"))
        {
            var reason = (dbgEventReason)Enum.Parse(typeof(dbgEventReason), message.Substring(14));
            mainThread.Dispatch(() => RunModeEntered?.Invoke(this, reason));
        }
        else if (message.StartsWith("DebuggingEnded"))
        {
            var reason = (dbgEventReason)Enum.Parse(typeof(dbgEventReason), message.Substring(14));
            mainThread.Dispatch(() => DebuggingEnded?.Invoke(this, reason));
        }
        else if (message.StartsWith("BreakModeEntered"))
        {
            var rest = message.Substring(16);
            var args = JsonConvert.DeserializeObject<IdeBreakpointHitEventArgs>(rest);
            mainThread.Dispatch(() => BreakModeEntered?.Invoke(this, args!));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        socket?.Close();
        socket = null;
    }

    public async Task<RemoteVisualStudioPacket> SendAsync(RemoteVisualStudioPacket packet)
    {
        var tcs = new TaskCompletionSource<RemoteVisualStudioPacket>();
        var timeOutTask = Task.Delay(timeOut);

        lock (this)
            pendingRequests.Add(tcs);

        await socket!.SendAsync(packet.ToBytes());
        var task = await Task.WhenAny(tcs.Task, timeOutTask);
        if (task == timeOutTask)
            throw new TimeoutException("Server did not respond in time");
        return await tcs.Task;
    }

    public async Task ConnectAsync()
    {
        async Task Loop(Task listenTask)
        {
            while (true)
            {
                if (disposed)
                    break;

                try
                {
                    await listenTask;
                }
                catch (Exception)
                {
                    if (disposed)
                        break;
                }

                await ConnectAsync();
            }
        }


        LOG.LogInformation($"Conecting to Visual Studio server... {host}:{port}");
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        await socket.ConnectAsync(host, port);

        var listenTask = Task.Run(Listen);
        var result = await SendAsync(new RemoteVisualStudioPacket(RemoteVisualStudioPacketType.Auth, key));
        if (result.Header.Type != RemoteVisualStudioPacketType.AuthOk)
        {
            await DisposeAsync();
            throw new Exception("Authentication failed");
        }

        Loop(listenTask).ListenErrors(); // no await!
    }

    public bool SkipSolutionPathValidation => true;

    public async Task<string?> GetSolutionFullPath() => (await SendAsync(RemoteVisualStudioPacket.Execute("GetSolutionFullPath"))).Message;

    public async Task<string> GetIdeName() => (await SendAsync(RemoteVisualStudioPacket.Execute("GetIdeName"))).Message;

    public Task DebugUnpause() => SendAsync(RemoteVisualStudioPacket.Execute("ExecDebuggerGo"));

    public Task DebugPause() => SendAsync(RemoteVisualStudioPacket.Execute("ExecDebuggerBreak"));

    public async Task ActivateWindow() => await SendAsync(RemoteVisualStudioPacket.Execute("ExecActivateWindow"));

    public async Task GoToFile(string fileName, int line, bool activate)
    {
        var activateStr = activate ? "1" : "0";
        await SendAsync(RemoteVisualStudioPacket.Execute($"ExecGoToFile|{fileName}|{line}|{activateStr}"));
    }

    public async Task<string?> GetLine(string fileName, int lineNumber) => (await SendAsync(RemoteVisualStudioPacket.Execute($"GetLine|{fileName}|{lineNumber}"))).Message;

    public event EventHandler<dbgEventReason>? RunModeEntered;

    public event EventHandler<IdeBreakpointHitEventArgs>? BreakModeEntered;

    public event EventHandler<dbgEventReason>? DebuggingEnded;

    public async Task<dbgDebugMode> GetDebugModeAsync()
    {
        var packet = await SendAsync(RemoteVisualStudioPacket.Execute("GetDebugMode"));
        return (dbgDebugMode)Enum.Parse(typeof(dbgDebugMode), packet.Message);
    }
}