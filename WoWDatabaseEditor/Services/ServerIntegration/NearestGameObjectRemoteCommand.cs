using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Services.ServerIntegration
{
    public class NearestGameObjectRemoteCommand : IRemoteCommand
    {
        public RemoteCommandPriority Priority => RemoteCommandPriority.Middle;

        public string GenerateCommand() => "dbeditor nearest_gobjects";

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            mergedCommand = null;
            return false;
        }
    }
}