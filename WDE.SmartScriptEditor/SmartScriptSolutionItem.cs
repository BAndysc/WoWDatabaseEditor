using System.Collections.ObjectModel;
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

        public bool IsContainer => false;

        public ObservableCollection<ISolutionItem> Items => null;

        public string ExtraId => Entry.ToString();

        public bool IsExportable => true;
    }
}