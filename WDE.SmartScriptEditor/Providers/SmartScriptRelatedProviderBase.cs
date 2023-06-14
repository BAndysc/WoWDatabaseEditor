using System;
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

        private RelatedSolutionItem.RelatedType SmartScriptToRelatedType(SmartScriptType type)
        {
            switch (type)
            {
                case SmartScriptType.Creature:
                    return RelatedSolutionItem.RelatedType.CreatureEntry;
                case SmartScriptType.GameObject:
                    return RelatedSolutionItem.RelatedType.GameobjectEntry;
                case SmartScriptType.Template:
                    return RelatedSolutionItem.RelatedType.Template;
                case SmartScriptType.TimedActionList:
                    return RelatedSolutionItem.RelatedType.TimedActionList;
                case SmartScriptType.Quest:
                    return RelatedSolutionItem.RelatedType.QuestEntry;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"{type} is not expected to be invoked here");
            }
        }
        
        public Task<RelatedSolutionItem?> GetRelated(T item)
        {
            if (item.SmartType != SmartScriptType.Creature &&
                item.SmartType != SmartScriptType.GameObject &&
                item.SmartType != SmartScriptType.Template &&
                item.SmartType != SmartScriptType.TimedActionList &&
                item.SmartType != SmartScriptType.Quest)
                return Task.FromResult<RelatedSolutionItem?>(null);

            if (item.Entry.HasValue)
            {
                return Task.FromResult<RelatedSolutionItem?>(
                    new RelatedSolutionItem(SmartScriptToRelatedType(item.SmartType), item.Entry.Value));
            }
            
            if (item.EntryOrGuid >= 0)
            {
                return Task.FromResult<RelatedSolutionItem?>(
                    new RelatedSolutionItem(SmartScriptToRelatedType(item.SmartType), item.EntryOrGuid));
            }

            if (item.SmartType == SmartScriptType.Creature)
            {
                var creature = databaseProvider.GetCreatureByGuid(0, (uint)(-item.EntryOrGuid));
                if (creature == null)
                    return Task.FromResult<RelatedSolutionItem?>(null);
                return Task.FromResult<RelatedSolutionItem?>(new RelatedSolutionItem(RelatedSolutionItem.RelatedType.CreatureEntry, creature.Entry));
            }
            
            var gameobject = databaseProvider.GetGameObjectByGuid(0, (uint)(-item.EntryOrGuid));
            if (gameobject == null)
                return Task.FromResult<RelatedSolutionItem?>(null);
            return Task.FromResult<RelatedSolutionItem?>(new RelatedSolutionItem(RelatedSolutionItem.RelatedType.GameobjectEntry, gameobject.Entry));
        }
    }
}