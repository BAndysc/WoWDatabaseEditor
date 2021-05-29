using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScript : SmartScriptBase
    {
        public SmartScript(ISmartScriptSolutionItem item,
            ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService) : base (smartFactory, smartDataManager, messageBoxService)
        {
            EntryOrGuid = (int) item.Entry;
            SourceType = item.SmartType;
        }

        public readonly int EntryOrGuid;
        public override SmartScriptType SourceType { get; }
    }
}