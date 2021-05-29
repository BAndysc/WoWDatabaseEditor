using WDE.Common.Services;

namespace WDE.Conditions
{
    public class ConditionsReloadRemoteCommand : IRemoteCommand
    {
        public static ConditionsReloadRemoteCommand Singleton { get; } = new();
        
        public string GenerateCommand() => "reload conditions";

        public RemoteCommandPriority Priority => RemoteCommandPriority.VeryFirst;

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand mergedCommand)
        {
            mergedCommand = Singleton;
            return true;
        }
    }
}