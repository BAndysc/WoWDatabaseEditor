using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScript
    {
        public readonly ObservableCollection<SmartEvent> Events;
        public readonly int EntryOrGuid;
        public readonly SmartScriptType SourceType;
        private readonly IUnityContainer _container;

        public SmartScript(SmartScriptSolutionItem item, IUnityContainer container)
        {
            EntryOrGuid = item.Entry;
            SourceType = item.SmartType;
            _container = container;
            Events = new ObservableCollection<SmartEvent>();
        }

        public void Load(IEnumerable<ISmartScriptLine> lines)
        {
            int? entry = null;
            SmartScriptType? source = null;
            int previousLink = -1;
            SmartEvent currentEvent = null;

            foreach (var line in lines)
            {
                if (!entry.HasValue)
                    entry = line.EntryOrGuid;
                else
                    Debug.Assert(entry.Value == line.EntryOrGuid);

                if (!source.HasValue)
                    source = (SmartScriptType)line.ScriptSourceType;
                else
                    Debug.Assert((int)source.Value == line.ScriptSourceType);

                if (previousLink != line.Id)
                {
                    currentEvent = _container.Resolve<ISmartFactory>().EventFactory(line);
                    Events.Add(currentEvent);
                }
                
                currentEvent.AddAction(_container.Resolve<ISmartFactory>().ActionFactory(line));

                previousLink = line.Link;
            }
        }
    }

}
