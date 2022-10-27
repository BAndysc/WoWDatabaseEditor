using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScript : SmartScriptBase
    {
        public SmartScript(ISmartScriptSolutionItem item,
            ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService,
            ISmartScriptImporter importer) : base (smartFactory, smartDataManager, messageBoxService, importer)
        {
            EntryOrGuid = (int) item.Entry;
            SourceType = item.SmartType;
        }

        public List<ISmartScriptLine> OriginalLines { get; } = new();
        
        public readonly int EntryOrGuid;
        public override SmartScriptType SourceType { get; }
    }
}