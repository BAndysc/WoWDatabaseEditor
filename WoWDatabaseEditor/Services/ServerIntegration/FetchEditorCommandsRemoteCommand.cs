using WDE.Common.Services;

namespace WoWDatabaseEditorCore.Services.ServerIntegration
{
    public class FetchEditorCommandsRemoteCommand : IRemoteCommand
    {
        public static FetchEditorCommandsRemoteCommand Instance { get; } = new();
        
        public RemoteCommandPriority Priority => RemoteCommandPriority.Middle;
        
        public string GenerateCommand() => "dbeditor client fetch";

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            mergedCommand = Instance;
            return true;
        }
    }
}