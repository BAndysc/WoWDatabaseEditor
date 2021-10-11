using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Providers
{
    public class SmartScriptRelatedProviderBase<T> : ISolutionItemRelatedProvider<T> where T : ISmartScriptSolutionItem
    {
        private readonly IDatabaseProvider databaseProvider;

        public SmartScriptRelatedProviderBase(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        public Task<RelatedSolutionItem?> GetRelated(T item)
        {
            if (item.SmartType != SmartScriptType.Creature &&
                item.SmartType != SmartScriptType.GameObject)
                return Task.FromResult<RelatedSolutionItem?>(null);

            if (item.Entry >= 0)
            {
                return Task.FromResult<RelatedSolutionItem?>(
                    new RelatedSolutionItem(item.SmartType == SmartScriptType.Creature ? 
                        RelatedSolutionItem.RelatedType.CreatureEntry : 
                        RelatedSolutionItem.RelatedType.GameobjectEntry, item.Entry));
            }

            if (item.SmartType == SmartScriptType.Creature)
            {
                var creature = databaseProvider.GetCreatureByGuid((uint)(-item.Entry));
                if (creature == null)
                    return Task.FromResult<RelatedSolutionItem?>(null);
                return Task.FromResult<RelatedSolutionItem?>(new RelatedSolutionItem(RelatedSolutionItem.RelatedType.CreatureEntry, creature.Entry));
            }
            
            var gameobject = databaseProvider.GetGameObjectByGuid((uint)(-item.Entry));
            if (gameobject == null)
                return Task.FromResult<RelatedSolutionItem?>(null);
            return Task.FromResult<RelatedSolutionItem?>(new RelatedSolutionItem(RelatedSolutionItem.RelatedType.GameobjectEntry, gameobject.Entry));
        }
    }
}