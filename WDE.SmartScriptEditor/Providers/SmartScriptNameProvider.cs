using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Solution;

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
            var Entry = item.Entry;

            if (Entry > 0)
            {
                switch (item.SmartType)
                {
                    case SmartScriptType.Creature:
                        var cr = database.GetCreatureTemplate((uint)Entry);
                        return cr == null || cr.Name == null ? "Creature " + Entry : cr.Name;
                    case SmartScriptType.GameObject:
                        var g = database.GetGameObjectTemplate((uint)Entry);
                        return g == null || g.Name == null ? "GameObject " + Entry : g.Name;
                    case SmartScriptType.AreaTrigger:
                        return "Areatrigger " + Entry;
                    case SmartScriptType.Quest:
                        var q = database.GetQuestTemplate((uint)Entry);
                        return q == null || q.Name == null ? "Quest " + Entry : q.Name;
                    case SmartScriptType.Spell:
                    case SmartScriptType.Aura:
                        if (spellStore.HasSpell((uint)Entry))
                            return spellStore.GetName((uint)Entry);
                        return (item.SmartType == SmartScriptType.Aura ? "Aura " : "Spell ") + Entry;
                    case SmartScriptType.TimedActionList:
                        return "Timed list " + Entry;
                    case SmartScriptType.Cinematic:
                        return "Cinematic " + Entry;
                }
            }

            return "Guid " + Entry;
        }
    }
}
