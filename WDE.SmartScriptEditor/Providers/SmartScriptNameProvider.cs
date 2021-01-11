using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.SmartScriptEditor.Providers
{
    [AutoRegister]
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
            var entry = item.Entry;

            if (entry > 0)
                switch (item.SmartType)
                {
                    case SmartScriptType.Creature:
                        var cr = database.GetCreatureTemplate((uint) entry);
                        return cr == null || cr.Name == null ? "Creature " + entry : cr.Name;
                    case SmartScriptType.GameObject:
                        var g = database.GetGameObjectTemplate((uint) entry);
                        return g == null || g.Name == null ? "GameObject " + entry : g.Name;
                    case SmartScriptType.AreaTrigger:
                        return "Areatrigger " + entry;
                    case SmartScriptType.Quest:
                        var q = database.GetQuestTemplate((uint) entry);
                        return q == null || q.Name == null ? "Quest " + entry : q.Name;
                    case SmartScriptType.Spell:
                    case SmartScriptType.Aura:
                        if (spellStore.HasSpell((uint) entry))
                            return spellStore.GetName((uint) entry);
                        return (item.SmartType == SmartScriptType.Aura ? "Aura " : "Spell ") + entry;
                    case SmartScriptType.TimedActionList:
                        return "Timed list " + entry;
                    case SmartScriptType.Cinematic:
                        return "Cinematic " + entry;
                }

            return "Guid " + entry;
        }
    }
}