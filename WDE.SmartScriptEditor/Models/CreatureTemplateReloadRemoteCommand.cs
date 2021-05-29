using System.Linq;
using WDE.Common.Services;

namespace WDE.SmartScriptEditor.Models
{
    public class CreatureTemplateReloadRemoteCommand : IRemoteCommand
    {
        private uint[] entries;
        
        public CreatureTemplateReloadRemoteCommand(uint entry)
        {
            entries = new[] {entry};
        }
        
        public CreatureTemplateReloadRemoteCommand(uint[] entries)
        {
            this.entries = entries;
        }
        
        public RemoteCommandPriority Priority => RemoteCommandPriority.First;

        public string GenerateCommand() => "reload creature_template " + string.Join(" ", entries);

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            if (other is CreatureTemplateReloadRemoteCommand remoteCommand)
            {
                var merged = entries.Union(remoteCommand.entries).ToArray();
                mergedCommand = new CreatureTemplateReloadRemoteCommand(merged);
                return true;
            }
            mergedCommand = null;
            return false;
        }
    }
}