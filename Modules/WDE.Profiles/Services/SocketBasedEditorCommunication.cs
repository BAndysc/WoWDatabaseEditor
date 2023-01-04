using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Profiles;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using Unit = System.Reactive.Unit;

namespace WDE.Profiles.Services;

[SingleInstance]
[AutoRegister]
public class SocketBasedEditorCommunication : IInterEditorCommunication
{
    private readonly IProjectItemSerializer serializer;

    private static class Messages
    {
        public const string BringToFront = "BringToFront";
        public const string Ping = "Ping";
        public const string Pong = "Pong";
        public const string Open = "Open:";
    }
    
    private int myPort;
    private Dictionary<ushort, int> failedToPing = new();
    private Dictionary<ushort, RunningProfile> knownProfiles = new(); // optimization to prevent opening files if not necessary
    private DirectoryInfo runningPath;

    private CancellationTokenSource? cancellationTokenSource;
    
    private ValuePublisher<Unit> bringToFrontRequest = new();
    public IObservable<Unit> BringToFrontRequest => bringToFrontRequest;
    
    private ValuePublisher<ISmartScriptProjectItem> openRequest = new();

    public IObservable<ISmartScriptProjectItem> OpenRequest => openRequest;

    public SocketBasedEditorCommunication(IProfilesFileSystem profilesFileSystem,
        IProjectItemSerializer serializer)
    {
        this.serializer = serializer;
        runningPath = profilesFileSystem.GetDirectoryPath("running");
        if (!runningPath.Exists)
            runningPath.Create();
    }
    
    public void Close()
    {
        cancellationTokenSource?.Cancel();
        CloseSelfInstance();
    }

    public void Open()
    {
        if (cancellationTokenSource != null)
            return;

        async Task Worker(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ServerLoop(token);
                }
                catch (Exception e)
                {
                    LOG.LogError(e, message: "Profile system crashed, restarting");
                }
            }
        }
        
        cancellationTokenSource = new();
        Worker(cancellationTokenSource.Token).ListenErrors();
        UpdateSelfInstance();
    }
    
    private async Task ServerLoop(CancellationToken token)
    {
        using var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        myPort = ((IPEndPoint)listener.Client.LocalEndPoint!).Port;
        var pong = Encoding.UTF8.GetBytes(Messages.Pong);
        UdpReceiveResult receive;
        while (true)
        {
            try
            {
                receive = await listener.ReceiveAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            
            if (token.IsCancellationRequested)
                return;
            
            var message = Encoding.UTF8.GetString(receive.Buffer);
         
            if (message == Messages.BringToFront)
                bringToFrontRequest.Publish(default);
            else if (message.StartsWith(Messages.Open))
            {
                var open = message.Substring(Messages.Open.Length);
                if (serializer.TryDeserializeItem(open, out var item))
                    openRequest.Publish(item);
            }
            else if (message == Messages.Ping)
                await listener.SendAsync(pong, pong.Length, receive.RemoteEndPoint);
        }
    }

    #region discover other editors
    
    private void UpdateSelfInstance()
    {
        // todo extract to a separate class
        string key = ProfileService.ReadDefaultProfileKey();
        try
        {
            File.WriteAllText(Path.Join(runningPath.FullName, myPort.ToString()), key);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
        }
    }

    public void CloseSelfInstance()
    {
        try
        {
            var path = new FileInfo(Path.Join(runningPath.FullName, myPort.ToString()));
            if (path.Exists)
                File.Delete(path.FullName);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
        }
    }

    public async Task<IList<RunningProfile>> GetRunning()
    {
        var now = DateTime.UtcNow;
        List<RunningProfile> runningProfiles = new();

        async Task CheckPort(string file, ushort port)
        {
            if (!knownProfiles.TryGetValue(port, out var profile))
            {
                var key = await File.ReadAllTextAsync(file);
                knownProfiles[port] = profile = new RunningProfile(key, port);
            }

            var pong = await SendMessageAndReceive(profile, Messages.Ping);

            if (pong == null)
            {
                if (failedToPing.TryGetValue(port, out var failedCount) && failedCount > 5)
                {
                    // dead
                    File.Delete(file);
                    knownProfiles.Remove(port);
                }
                else
                {
                    failedToPing[port] = failedCount + 1;
                }
            }
            else
            {
                runningProfiles.Add(profile);
                failedToPing[port] = 0;
            }
        }
        
        List<Task> tasks = new();
        
        foreach (var file in Directory.GetFiles(runningPath.FullName))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!ushort.TryParse(fileName, out var port))
                continue;

            tasks.Add(CheckPort(file, port));
        }

        await Task.WhenAll(tasks.ToArray());

        return runningProfiles;
    }

    #endregion

    private async Task SendMessage(RunningProfile instance, string message)
    {
        try
        {
            using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            var bytes = Encoding.UTF8.GetBytes(message);
            await udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, instance.Port));
        }
        catch (Exception e)
        {
            LOG.LogError(e);
        }
    }
    
    private async Task<string?> SendMessageAndReceive(RunningProfile instance, string message)
    {
        try
        {
            using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            var bytes = Encoding.UTF8.GetBytes(message);
            await udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, instance.Port));
            var receiveTask = udpClient.ReceiveAsync();
            
            if (await Task.WhenAny(receiveTask, Task.Delay(200)) == receiveTask)
            {
                await receiveTask;
                return Encoding.UTF8.GetString(receiveTask.Result.Buffer);
            }
            else
            {
                receiveTask.IgnoreResult();
                return null; // timeout
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }
    
    public void RequestActivate(RunningProfile instance)
    {
        SendMessage(instance, Messages.BringToFront).ListenErrors();
    }
    
    public void RequestOpen(RunningProfile instance, ISmartScriptProjectItem projectItem)
    {
        SendMessage(instance, Messages.Open + serializer.Serialize(projectItem)).ListenErrors();
    }

}