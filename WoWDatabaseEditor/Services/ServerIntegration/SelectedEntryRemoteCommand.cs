using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Services.ServerIntegration
{
    public class SelectedEntryRemoteCommand : IRemoteCommand
    {
        public RemoteCommandPriority Priority => RemoteCommandPriority.Middle;

        public string GenerateCommand() => "dbeditor selected_entry";

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            mergedCommand = null;
            return false;
        }
    }
}