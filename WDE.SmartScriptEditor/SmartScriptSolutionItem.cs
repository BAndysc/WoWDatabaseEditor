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
using WDE.Common.Solution;

namespace WDE.SmartScriptEditor
{
    public class SmartScriptSolutionItem : ISolutionItem
    {
        public int Entry { get; }
        public SmartScriptType SmartType { get; set; }

        public SmartScriptSolutionItem(int entry, SmartScriptType type)
        {
            Entry = entry;
            SmartType = type;
        }

        public bool IsContainer => false;

        public ObservableCollection<ISolutionItem> Items => null;

        public string ExtraId => Entry.ToString();

        public bool IsExportable => true;
        
        public string ExportSql(ISolutionItemSqlGeneratorRegistry registry)
        {
            return registry.GenerateSql(this);
        }
    }
}
