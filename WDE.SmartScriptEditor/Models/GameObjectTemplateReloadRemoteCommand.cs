using WDE.Common.Services;

namespace WDE.SmartScriptEditor.Models
{
    public class GameObjectTemplateReloadRemoteCommand : IRemoteCommand
    {
        public RemoteCommandPriority Priority => RemoteCommandPriority.Middle;
        public static IRemoteCommand Singleton { get; } = new GameObjectTemplateReloadRemoteCommand();

        public string GenerateCommand() => "reload gameobject_template";

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            mergedCommand = Singleton;
            return true;
        }
    }
}