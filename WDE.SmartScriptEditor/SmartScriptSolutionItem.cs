using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptSolutionItem : ISolutionItem, ISmartScriptSolutionItem
    {
        public SmartScriptSolutionItem(int entry, SmartScriptType type)
        {
            Entry = entry;
            SmartType = type;
        }

        public int Entry { get; }
        public SmartScriptType SmartType { get; set; }
        public void UpdateDependants(HashSet<long> usedTimed)
        {
            for (int i = Items.Count - 1; i >= 0; --i)
            {
                if (!usedTimed.Contains(((SmartScriptSolutionItem) Items[i]).Entry))
                    Items.RemoveAt(i);
                else
                    usedTimed.Remove(((SmartScriptSolutionItem) Items[i]).Entry);
            }

            foreach (var t in usedTimed)
            {
                Items.Add(new SmartScriptSolutionItem((int)t, SmartScriptType.TimedActionList));
            }

        }

        [JsonIgnore]
        public bool IsContainer { get; set; }

        [JsonIgnore] 
        public ObservableCollection<ISolutionItem> Items { get; set; } = new();

        public string ExtraId => Entry.ToString();

        [JsonIgnore]
        public bool IsExportable => true;
    }
}