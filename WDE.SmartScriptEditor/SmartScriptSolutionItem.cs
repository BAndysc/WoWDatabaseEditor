using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;
using Prism.Ioc;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptSolutionItem : ISolutionItem
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ISmartFactory smartFactory;
        private readonly IDatabaseProvider database;
        private readonly ISpellStore spellStore;

        public int Entry { get; }
        public SmartScriptType SmartType { get; set; }

        public SmartScriptSolutionItem(int entry, SmartScriptType type, IEventAggregator eventAggregator, ISmartFactory smartFactory, IDatabaseProvider database, ISpellStore spellStore)
        {
            Entry = entry;
            SmartType = type;
            this.eventAggregator = eventAggregator;
            this.smartFactory = smartFactory;
            this.database = database;
            this.spellStore = spellStore;
        }

        public bool IsContainer => false;

        public ObservableCollection<ISolutionItem> Items => null;

        public string Name
        {
            get
            {
                return SmartType.ToString() + " " + Entry.ToString();
                if (Entry > 0)
                {
                    switch (SmartType)
                    {
                        case SmartScriptType.Creature:
                            var cr = database.GetCreatureTemplate((uint) Entry);
                            return cr == null || cr.Name==null ? "Creature "+Entry : cr.Name;
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
                            Debug.Assert(Entry >= 0);
                            if (spellStore.HasSpell((uint)Entry))
                                return spellStore.GetName((uint)Entry);
                            return (SmartType==SmartScriptType.Aura?"Aura ":"Spell ") + Entry;
                        case SmartScriptType.Timed:
                            return "Timed list " + Entry;
                        case SmartScriptType.Cinematic:
                            return "Cinematic " + Entry;
                    }
                }

                return "Guid " + Entry;
            }
        }
        public string ExtraId => Entry.ToString();

        public bool IsExportable => true;

        public string ExportSql
        {
            get
            {
                EventRequestGenerateSqlArgs args = new EventRequestGenerateSqlArgs();
                args.Item = this;

                eventAggregator.GetEvent<EventRequestGenerateSql>().Publish(args);

                if (args.Sql != null)
                    return args.Sql;
                    
                SmartScript script = new SmartScript(this, smartFactory);
                script.Load(database.GetScriptFor(Entry, SmartType));
                return new SmartScriptExporter(script, smartFactory).GetSql();
            }
        }
    }
}
