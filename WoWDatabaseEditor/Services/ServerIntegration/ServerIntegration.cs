using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.ServerIntegration
{
    [SingleInstance]
    [AutoRegister]
    public class ServerIntegration : IServerIntegration
    {
        private readonly IRemoteConnectorService remoteConnectorService;
        private readonly IStatusBar statusBar;
        private readonly Lazy<IWindowManager> windowManager;
        private readonly IMainThread mainThread;

        private List<ReverseRemoteCommandHolder> commands = new();

        public ServerIntegration(IRemoteConnectorService remoteConnectorService,
            IStatusBar statusBar, Lazy<IWindowManager> windowManager,
            IMainThread mainThread,
            ICurrentCoreVersion currentCoreVersion,
            IEnumerable<IReverseRemoteCommand> reverseRemoteCommands,
            IEnumerable<IRawReverseRemoteCommand> rawReverseRemoteCommands)
        {
            this.remoteConnectorService = remoteConnectorService;
            this.statusBar = statusBar;
            this.windowManager = windowManager;
            this.mainThread = mainThread;

            foreach (var command in reverseRemoteCommands)
            {
                var atr = command.GetType().GetCustomAttribute(typeof(ReverseRemoteCommandAttribute), true) as ReverseRemoteCommandAttribute;
                if (atr == null)
                    throw new Exception($"Found new ReverseRemoteCommand {command.GetType()}, but it doesn't have ReverseRemoteCommandAttribute, skipping");
             
                commands.Add(new(){Name = atr.Name, Command = command});
            }

            foreach (var command in rawReverseRemoteCommands)
            {
                var atr = command.GetType().GetCustomAttribute(typeof(ReverseRemoteCommandAttribute), true) as ReverseRemoteCommandAttribute;
                if (atr == null)
                    throw new Exception($"Found new ReverseRemoteCommand {command.GetType()}, but it doesn't have ReverseRemoteCommandAttribute, skipping");

                commands.Add(new(){Name = atr.Name, RawCommand = command});
            }

            remoteConnectorService.EditorCommandReceived += RemoteConnectorService_EditorCommandReceived;
        }

        private void RemoteConnectorService_EditorCommandReceived(string line)
        {
            async Task ProcessAsync()
            {
                bool anyCmd = false;
                bool broughtEditorToFront = false;
                foreach (var cmd in commands)
                {
                    if (line.StartsWith(cmd.Name))
                    {
                        if (line.Length == cmd.Name.Length || line[cmd.Name.Length] == ' ')
                        {
                            var args = line.Substring(cmd.Name.Length).Trim();
                            var bringToFront = (cmd.Command?.BringEditorToFront ?? cmd.RawCommand?.BringEditorToFront) ?? false;
                            if (bringToFront && !broughtEditorToFront)
                            {
                                mainThread.Dispatch(() => windowManager.Value.Activate());
                                broughtEditorToFront = true;
                            }
                            if (cmd.Command != null)
                               await cmd.Command.Invoke(new CommandArguments(args));
                            if (cmd.RawCommand != null)
                                await cmd.RawCommand.Invoke(args);
                            anyCmd = true;
                            break;
                        }
                    }
                }
                if (!anyCmd)
                    LOG.LogWarning("Got command " + line + " but didn't found any command to process this");
            }
            ProcessAsync().ListenErrors();
        }

        private async Task<string?> Invoke(IRemoteCommand remoteCommand)
        {
            if (!remoteConnectorService.IsConnected)
                return null;
            
            try
            {
                var response = (await remoteConnectorService.ExecuteCommand(remoteCommand)).Trim();

                if (response.StartsWith("### USAGE:"))
                    statusBar.PublishNotification(new PlainNotification(NotificationType.Warning,
                        "Debug commands disabled in config"));
                else if (response == "NO_PLAYER")
                    statusBar.PublishNotification(new PlainNotification(NotificationType.Warning,
                        "No player found on the server"));

                return response;
            }
            catch (CouldNotConnectToRemoteServer)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                    "Couldn't connect to the remote server. Check remote server connection settings or check if the server is alive." ));
                return null;
            }
            catch (Exception e)
            {
                LOG.LogError(e);
                return null;
            }
        }
        
        public async Task<uint?> GetSelectedEntry()
        {
            var response = await Invoke(new SelectedEntryRemoteCommand());

            if (response is null)
                return null;
            
            if (uint.TryParse(response, out var entry))
                return entry;
            
            if (response == "NO_SELECTED")
                statusBar.PublishNotification(new PlainNotification(NotificationType.Warning, "No creature is selected"));

            return null;
        }

        public async Task<IList<NearestGameObject>?> GetNearestGameObjects()
        {
            var response = await Invoke(new NearestGameObjectRemoteCommand());

            if (response is null)
                return null;

            if (response == "NO_OBJECTS")
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Warning, "No creature is selected"));
                return null;
            }

            List<NearestGameObject> nearest = new();
                
            foreach (var line in response.Split('\n'))
            {
                var parts = line.Trim().Split(';');
                if (parts.Length != 5)
                    continue;

                if (!uint.TryParse(parts[0], out var entry) ||
                    !float.TryParse(parts[1], out var dist) ||
                    !float.TryParse(parts[2], out var x) ||
                    !float.TryParse(parts[3], out var y) ||
                    !float.TryParse(parts[4], out var z))
                    continue;
                    
                nearest.Add(new NearestGameObject()
                {
                    Distance = dist,
                    X = x,
                    Y = y,
                    Z = z,
                    Entry = entry
                });
            }

            return nearest;
        }

        private struct ReverseRemoteCommandHolder
        {
            public string Name { get; init; }
            public IReverseRemoteCommand? Command
            {
                get;
                init;
            }
            public IRawReverseRemoteCommand? RawCommand
            {
                get;
                init;
            }
        }
    }
}