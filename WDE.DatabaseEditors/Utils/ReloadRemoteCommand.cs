using WDE.Common.Services;

namespace WDE.DatabaseEditors.Utils
{
    public class ReloadRemoteCommand : IRemoteCommand
    {
        private readonly string remoteCommand;

        public ReloadRemoteCommand(string remoteCommand)
        {
            this.remoteCommand = remoteCommand;
        }
        
        public string GenerateCommand()
        {
            return remoteCommand;
        }

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