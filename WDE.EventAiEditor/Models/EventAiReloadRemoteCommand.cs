using WDE.Common.Services;

namespace WDE.EventAiEditor.Models
{
    public class EventAiReloadRemoteCommand : IRemoteCommand
    {
        public static EventAiReloadRemoteCommand Singleton { get; } = new();
        
        public string GenerateCommand() => "reload all_eventai";

        public RemoteCommandPriority Priority => RemoteCommandPriority.VeryFirst;

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand mergedCommand)
        {
            mergedCommand = Singleton;
            return true;
        }
    }
}