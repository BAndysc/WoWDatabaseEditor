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
                case SmartScriptType.StaticSpell:
                case SmartScriptType.Spell:
                    return RelatedSolutionItem.RelatedType.Spell;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), $"{type} is not expected to be invoked here");
            }
        }
        
        public async Task<RelatedSolutionItem?> GetRelated(T item)
        {
            if (item.SmartType != SmartScriptType.Creature &&
                item.SmartType != SmartScriptType.GameObject &&
                item.SmartType != SmartScriptType.Template &&
                item.SmartType != SmartScriptType.TimedActionList &&
                item.SmartType != SmartScriptType.Quest &&
                item.SmartType != SmartScriptType.Spell &&
                item.SmartType != SmartScriptType.StaticSpell)
                return null;

            if (item.Entry.HasValue)
            {
                return new RelatedSolutionItem(SmartScriptToRelatedType(item.SmartType), item.Entry.Value);
            }

            if (item.EntryOrGuid >= 0)
            {
                return new RelatedSolutionItem(SmartScriptToRelatedType(item.SmartType), item.EntryOrGuid);
            }

            if (item.SmartType == SmartScriptType.Creature)
            {
                var creature = await databaseProvider.GetCreatureByGuidAsync(0, (uint)(-item.EntryOrGuid));
                if (creature == null)
                    return null;
                return new RelatedSolutionItem(RelatedSolutionItem.RelatedType.CreatureEntry, creature.Entry);
            }
            
            var gameobject = await databaseProvider.GetGameObjectByGuidAsync(0, (uint)(-item.EntryOrGuid));
            if (gameobject == null)
                return null;
            return new RelatedSolutionItem(RelatedSolutionItem.RelatedType.GameobjectEntry, gameobject.Entry);
        }
    }
}
