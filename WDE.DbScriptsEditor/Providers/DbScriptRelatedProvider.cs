using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;
using static WDE.Common.Solution.RelatedSolutionItem;

namespace WDE.DbScriptsEditor.Providers
{
    // Links a dbscript to the game entity its id represents, so the editor can surface the related
    // creature/GO/quest/spell (and vice versa). Only script types whose id IS an entity map here;
    // free-id types (relay/event/gossip/movement) have no single owning entity.
    [AutoRegister]
    [SingleInstance]
    public class DbScriptRelatedProvider : ISolutionItemRelatedProvider<DbScriptSolutionItem>
    {
        public Task<RelatedSolutionItem?> GetRelated(DbScriptSolutionItem item)
        {
            RelatedSolutionItem? related = item.ScriptType switch
            {
                DbScriptType.CreatureDeath => new RelatedSolutionItem(RelatedType.CreatureEntry, item.ScriptId),
                DbScriptType.GoTemplateUse => new RelatedSolutionItem(RelatedType.GameobjectEntry, item.ScriptId),
                DbScriptType.QuestStart => new RelatedSolutionItem(RelatedType.QuestEntry, item.ScriptId),
                DbScriptType.QuestEnd => new RelatedSolutionItem(RelatedType.QuestEntry, item.ScriptId),
                DbScriptType.Spell => new RelatedSolutionItem(RelatedType.Spell, item.ScriptId),
                _ => null,
            };
            return Task.FromResult(related);
        }
    }
}
