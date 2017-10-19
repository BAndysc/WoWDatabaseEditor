using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Events;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptSolutionItem : ISolutionItem
    {
        private IUnityContainer _unity;

        public int Entry { get; }
        public SmartScriptType SmartType { get; set; }

        public SmartScriptSolutionItem(int entry, SmartScriptType type, IUnityContainer unity)
        {
            Entry = entry;
            SmartType = type;
            _unity = unity;
        }

        public bool IsContainer => false;

        public ObservableCollection<ISolutionItem> Items => null;

        public string Name
        {
            get
            {
                if (Entry > 0)
                {
                    switch (SmartType)
                    {
                        case SmartScriptType.Creature:
                            var cr = _unity.Resolve<IDatabaseProvider>().GetCreatureTemplate((uint) Entry);
                            return cr == null || cr.Name==null ? "Creature "+Entry : cr.Name;
                        case SmartScriptType.GameObject:
                            var g = _unity.Resolve<IDatabaseProvider>().GetGameObjectTemplate((uint)Entry);
                            return g == null || g.Name == null ? "GameObject " + Entry : g.Name;
                        case SmartScriptType.AreaTrigger:
                            return "Areatrigger " + Entry;
                        case SmartScriptType.Quest:
                            var q = _unity.Resolve<IDatabaseProvider>().GetQuestTemplate((uint)Entry);
                            return q == null || q.Name == null ? "Quest " + Entry : q.Name;
                        case SmartScriptType.Spell:
                        case SmartScriptType.Aura:
                            var dict = _unity.Resolve<IDbcStore>().SpellStore;
                            if (dict.ContainsKey(Entry))
                                return dict[Entry];
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
        public void SetUnity(IUnityContainer unity)
        {
            _unity = unity;
        }

        public bool IsExportable => true;

        public string ExportSql
        {
            get
            {
                var ea = _unity.Resolve<IEventAggregator>();

                EventRequestGenerateSqlArgs args = new EventRequestGenerateSqlArgs();
                args.Item = this;

                ea.GetEvent<EventRequestGenerateSql>().Publish(args);

                if (args.Sql != null)
                    return args.Sql;
                    
                SmartScript script = new SmartScript(this, _unity);
                script.Load(_unity.Resolve<IDatabaseProvider>().GetScriptFor(Entry, SmartType));
                return new SmartScriptExporter(script, _unity).GetSql();
            }
        }
    }
}
