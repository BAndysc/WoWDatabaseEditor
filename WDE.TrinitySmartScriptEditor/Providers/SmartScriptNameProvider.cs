using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptNameProvider : ISolutionNameProvider<SmartScriptSolutionItem>
    {
        private readonly IDatabaseProvider database;
        private readonly ISpellStore spellStore;

        public SmartScriptNameProvider(IDatabaseProvider database, ISpellStore spellStore)
        {
            this.database = database;
            this.spellStore = spellStore;
        }

        public string GetName(SmartScriptSolutionItem item)
        {
            int entry = (int)item.Entry;

            if (entry > 0)
            {
                switch (item.SmartType)
                {
                    case SmartScriptType.Creature:
                        ICreatureTemplate cr = database.GetCreatureTemplate((uint) entry);
                        return cr == null || cr.Name == null ? "Creature " + entry : cr.Name;
                    case SmartScriptType.GameObject:
                        IGameObjectTemplate g = database.GetGameObjectTemplate((uint) entry);
                        return g == null || g.Name == null ? "GameObject " + entry : g.Name;
                    case SmartScriptType.AreaTrigger:
                        return "Clientside area trigger " + entry;
                    case SmartScriptType.Quest:
                        IQuestTemplate q = database.GetQuestTemplate((uint) entry);
                        return q == null || q.Name == null ? "Quest " + entry : q.Name;
                    case SmartScriptType.Spell:
                    case SmartScriptType.Aura:
                        if (spellStore.HasSpell((uint) entry))
                            return spellStore.GetName((uint) entry);
                        return (item.SmartType == SmartScriptType.Aura ? "Aura " : "Spell ") + entry;
                    case SmartScriptType.TimedActionList:
                        return "Timed list " + entry;
                    case SmartScriptType.AreaTriggerEntity:
                        return "Area trigger entity " + entry;
                    case SmartScriptType.AreaTriggerEntityServerSide:
                        return "Serverside area trigger entity " + entry;
                }
            }

            return "Guid " + entry;
        }
    }
}