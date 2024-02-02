using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScript : SmartScriptBase
    {
        public IEditorFeatures EditorFeatures { get; }

        public SmartScript(ISmartScriptSolutionItem item,
            ISmartFactory smartFactory,
            ISmartDataManager smartDataManager,
            IMessageBoxService messageBoxService,
            IEditorFeatures editorFeatures,
            ISmartScriptImporter importer) : base (smartFactory, editorFeatures, smartDataManager, messageBoxService, importer)
        {
            EditorFeatures = editorFeatures;
            EntryOrGuid = (int) item.EntryOrGuid;
            Entry = item.Entry;
            SourceType = item.SmartType;
        }

        public List<ISmartScriptLine> OriginalLines { get; } = new();

        public readonly uint? Entry;
        public readonly int EntryOrGuid;
        public override SmartScriptType SourceType { get; }

        public override int GetFirstPossibleTimedActionListId() =>
            Math.Abs(EntryOrGuid) * 100;
    }
}