using WDE.Common.Services;

namespace WDE.DatabaseEditors.Utils
{
    public class ReloadRemoteCommand : IRemoteCommand
    {
        private readonly string remoteCommand;
        private readonly RemoteCommandPriority priority;

        public ReloadRemoteCommand(string remoteCommand, RemoteCommandPriority priority)
        {
            this.remoteCommand = remoteCommand;
            this.priority = priority;
        }
        
        public string GenerateCommand()
        {
            return remoteCommand;
        }

        public RemoteCommandPriority Priority => priority;

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            mergedCommand = null;
            if (other is ReloadRemoteCommand same && same.remoteCommand == remoteCommand)
            {
                mergedCommand = other;
                return true;
            }

            return false;
        }
    }
}