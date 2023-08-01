using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor
{
    public abstract class SmartScriptIconBaseProvider<T> : ISolutionItemIconProvider<T> where T : ISmartScriptSolutionItem
    {
        private readonly ICachedDatabaseProvider databaseProvider;
        private readonly ICurrentCoreVersion currentCoreVersion;

        public SmartScriptIconBaseProvider(ICachedDatabaseProvider databaseProvider, ICurrentCoreVersion currentCoreVersion)
        {
            this.databaseProvider = databaseProvider;
            this.currentCoreVersion = currentCoreVersion;
        }

        public virtual ImageUri GetIcon(T item)
        {
            switch (item.SmartType)
            {
                case SmartScriptType.Creature:
                    return new ImageUri("Icons/document_creature.png");
                case SmartScriptType.GameObject:
                    return new ImageUri("Icons/document_gobject.png");
                case SmartScriptType.AreaTrigger:
                    return new ImageUri("Icons/document_areatrigger.png");
                case SmartScriptType.Event:
                    return new ImageUri("Icons/document_event.png");
                case SmartScriptType.Gossip:
                    return new ImageUri("Icons/document.png");
                case SmartScriptType.Quest:
                {
                    var template = databaseProvider.GetCachedQuestTemplate((uint)item.EntryOrGuid);
                    if (template != null && template.AllowableRaces != 0)
                    {
                        var onlyHorde = (template.AllowableRaces & ~ (CharacterRaces.AllHorde)) == 0;
                        var onlyAlliance = (template.AllowableRaces & ~ (CharacterRaces.AllAlliance)) == 0;
                        if (onlyHorde)
                            return new ImageUri("Icons/document_quest_horde.png");
                        if (onlyAlliance)
                            return new ImageUri("Icons/document_quest_ally.png");
                    }
                    return new ImageUri("Icons/document_quest_2.png");
                }
                case SmartScriptType.Spell:
                    return new ImageUri("Icons/document_spell.png");
                case SmartScriptType.Transport:
                    return new ImageUri("Icons/document.png");
                case SmartScriptType.Instance:
                    return new ImageUri("Icons/document.png");
                case SmartScriptType.TimedActionList:
                    return new ImageUri("Icons/document_timedactionlist.png");
                case SmartScriptType.Scene:
                    return new ImageUri("Icons/document_cinematic.png");
                case SmartScriptType.AreaTriggerEntity:
                    return new ImageUri("Icons/document_areatrigger.png");
                case SmartScriptType.AreaTriggerEntityServerSide:
                    return new ImageUri("Icons/document_areatrigger.png");
                case SmartScriptType.Aura:
                    return new ImageUri("Icons/document_aura.png");
                case SmartScriptType.Cinematic:
                    return new ImageUri("Icons/document_cinematic.png");
                case SmartScriptType.ActionList:
                    return new ImageUri("Icons/document_actionlist.png");
                case SmartScriptType.PlayerChoice:
                    return new ImageUri("Icons/document_player_choice.png");
                case SmartScriptType.Template:
                    return new ImageUri("Icons/document_template.png");
                case SmartScriptType.StaticSpell:
                    return new ImageUri("Icons/document_spell.png");
                case SmartScriptType.BattlePet:
                    return new ImageUri("Icons/document_battle_pet.png");
                case SmartScriptType.Conversation:
                    return new ImageUri("Icons/document_conversation.png");
                default:
                    return new ImageUri("Icons/document.png");
            }
        }
    }
}