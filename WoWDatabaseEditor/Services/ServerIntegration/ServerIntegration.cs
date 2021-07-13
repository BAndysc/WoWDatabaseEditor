using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.ServerIntegration
{
    [SingleInstance]
    [AutoRegister]
    public class ServerIntegration : IServerIntegration
    {
        private readonly IRemoteConnectorService remoteConnectorService;
        private readonly IStatusBar statusBar;

        public ServerIntegration(IRemoteConnectorService remoteConnectorService,
            IStatusBar statusBar)
        {
            this.remoteConnectorService = remoteConnectorService;
            this.statusBar = statusBar;
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
            catch (CouldNotConnectToRemoteServer _)
            {
                statusBar.PublishNotification(new PlainNotification(NotificationType.Error,
                    "Couldn't connect to the remote server. Check remote server connection settings or check if the server is alive." ));
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
    }
}