using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Providers
{
    public class SmartScriptNameProviderBase<T> : ISolutionNameProvider<T> where T : ISmartScriptSolutionItem
    {
        private readonly IDatabaseProvider database;
        private readonly ISpellStore spellStore;

        public SmartScriptNameProviderBase(IDatabaseProvider database, ISpellStore spellStore)
        {
            this.database = database;
            this.spellStore = spellStore;
        }

        private string? TryGetName(int entryOrGuid, SmartScriptType type)
        {
            uint? entry = 0;
            switch (type)
            {
                case SmartScriptType.Creature:
                    if (entryOrGuid < 0)
                        entry = database.GetCreatureByGuid((uint)-entryOrGuid)?.Entry;
                    else
                        entry = (uint)entryOrGuid;
                    
                    if (entry.HasValue)
                        return database.GetCreatureTemplate(entry.Value)?.Name;
                    break;
                case SmartScriptType.GameObject:
                    if (entryOrGuid < 0)
                        entry = database.GetGameObjectByGuid((uint)-entryOrGuid)?.Entry;
                    else
                        entry = (uint)entryOrGuid;
                    
                    if (entry.HasValue)
                        return database.GetGameObjectTemplate(entry.Value)?.Name;
                    break;
                case SmartScriptType.Quest:
                    return database.GetQuestTemplate((uint)entryOrGuid)?.Name;
                case SmartScriptType.Aura:
                case SmartScriptType.Spell:
                    if (spellStore.HasSpell((uint) entryOrGuid))
                        return spellStore.GetName((uint) entryOrGuid);
                    break;
                default:
                    return null;
            }

            return null;
        }
        
        public virtual string GetName(T item)
        {
            var name = TryGetName(item.Entry, item.SmartType);
            if (!string.IsNullOrEmpty(name))
            {
                if (item.Entry < 0 && (item.SmartType == SmartScriptType.Creature || item.SmartType == SmartScriptType.GameObject))
                    return name + " with guid " + -item.Entry;
                return name;
            }
            
            int entry = item.Entry;

            if (entry > 0)
            {
                switch (item.SmartType)
                {
                    case SmartScriptType.Creature:
                        return "Creature " + entry;
                    case SmartScriptType.GameObject:
                        return "GameObject " + entry;
                    case SmartScriptType.AreaTrigger:
                        return "Clientside area trigger " + entry;
                    case SmartScriptType.Quest:
                        return "Quest " + entry;
                    case SmartScriptType.Spell:
                    case SmartScriptType.Aura:
                        return (item.SmartType == SmartScriptType.Aura ? "Aura " : "Spell ") + entry;
                    case SmartScriptType.TimedActionList:
                        return "Timed list " + entry;
                    case SmartScriptType.AreaTriggerEntity:
                        return "Area trigger entity " + entry;
                    case SmartScriptType.AreaTriggerEntityServerSide:
                        return "Serverside area trigger entity " + entry;
                }
            }

            if (item.SmartType == SmartScriptType.Creature)
                return "Creature with guid " + -entry;
            
            if (item.SmartType == SmartScriptType.GameObject)
                return "GameObject with guid " + -entry;
            
            return "Guid " + -entry;
        }
    }
}