using System;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Providers
{
    public class SmartScriptNameProviderBase<T> : ISolutionNameProvider<T> where T : ISmartScriptSolutionItem
    {
        private readonly ICachedDatabaseProvider database;
        private readonly ISpellStore spellStore;
        private readonly IDbcStore dbcStore;
        private readonly IParameterFactory parameterFactory;

        public SmartScriptNameProviderBase(ICachedDatabaseProvider database, 
            ISpellStore spellStore, 
            IDbcStore dbcStore,
            IParameterFactory parameterFactory)
        {
            this.database = database;
            this.spellStore = spellStore;
            this.dbcStore = dbcStore;
            this.parameterFactory = parameterFactory;
        }

        protected virtual string? TryGetName(uint? scriptEntry, int entryOrGuid, SmartScriptType type)
        {
            uint? entry = 0;
            switch (type)
            {
                case SmartScriptType.Creature:
                    if (entryOrGuid < 0)
                        entry = database.GetCachedCreatureByGuid(0, (uint)-entryOrGuid)?.Entry;
                    else if (scriptEntry.HasValue)
                        entry = scriptEntry.Value;
                    else
                        entry = (uint)entryOrGuid;
                    
                    if (entry.HasValue)
                        return database.GetCachedCreatureTemplate(entry.Value)?.Name;
                    break;
                case SmartScriptType.GameObject:
                    if (entryOrGuid < 0)
                        entry = database.GetCachedGameObjectByGuid(0, (uint)-entryOrGuid)?.Entry;
                    else if (scriptEntry.HasValue)
                        entry = scriptEntry.Value;
                    else
                        entry = (uint)entryOrGuid;
                    
                    if (entry.HasValue)
                        return database.GetCachedGameObjectTemplate(entry.Value)?.Name;
                    break;
                case SmartScriptType.Quest:
                    return database.GetCachedQuestTemplate((uint)entryOrGuid)?.Name;
                case SmartScriptType.Aura:
                case SmartScriptType.Spell:
                case SmartScriptType.StaticSpell:
                    if (spellStore.HasSpell((uint) entryOrGuid))
                        return spellStore.GetName((uint) entryOrGuid);
                    break;
                case SmartScriptType.Scene:
                    entry = database.GetCachedSceneTemplate((uint)entryOrGuid)?.ScriptPackageId;

                    if (entry.HasValue && dbcStore.SceneStore.ContainsKey((uint)entry))
                        return dbcStore.SceneStore[(uint)entry];
                    break;
                case SmartScriptType.BattlePet:
                    if (dbcStore.BattlePetSpeciesIdStore?.TryGetValue(entryOrGuid, out var creatureId) ?? false)
                    {
                        if (database.GetCachedCreatureTemplate((uint)creatureId) is { } battleCreature)
                            return battleCreature.Name;
                    }
                    break;
                case SmartScriptType.AreaTriggerEntityServerSide:
                    var param = parameterFactory.Factory("ServersideAreatriggerParameter");
                    if (param.Items?.TryGetValue(entryOrGuid, out var name) ?? false)
                        return name.Name;
                    break;
                case SmartScriptType.Conversation:
                    var conversation = parameterFactory.Factory("ConversationParameter");
                    if (conversation.Items?.TryGetValue(entryOrGuid, out var conversationName) ?? false)
                        return conversationName.Name;
                    break;
                default:
                    return null;
            }

            return null;
        }
        
        public virtual string GetName(T item)
        {
            var name = TryGetName(item.Entry, item.EntryOrGuid, item.SmartType);
            if (!string.IsNullOrEmpty(name))
            {
                if (item.EntryOrGuid < 0 && (item.SmartType == SmartScriptType.Creature || item.SmartType == SmartScriptType.GameObject))
                    return name + " with guid " + -item.EntryOrGuid;
                return name;
            }
            
            int entry = item.EntryOrGuid;

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
                    case SmartScriptType.Event:
                        return "Event " + entry;
                    case SmartScriptType.Gossip:
                        return "Gossip " + entry;
                    case SmartScriptType.Transport:
                        return "Transport " + entry;
                    case SmartScriptType.Instance:
                        return "Instance " + entry;
                    case SmartScriptType.Scene:
                        return "Scene " + entry;
                    case SmartScriptType.Cinematic:
                        return "Cinematic " + entry;
                    case SmartScriptType.PlayerChoice:
                        return "Player choice " + entry;
                    case SmartScriptType.Template:
                        return "Template " + entry;
                    case SmartScriptType.StaticSpell:
                        return "Static spell " + entry;
                    case SmartScriptType.BattlePet:
                        return "Battle pet " + entry;
                    case SmartScriptType.Conversation:
                        return "Conversation " + entry;
                    default:
                        throw new ArgumentOutOfRangeException();
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