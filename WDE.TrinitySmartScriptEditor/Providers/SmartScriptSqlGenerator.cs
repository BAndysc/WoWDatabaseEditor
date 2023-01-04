using System;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptSqlGenerator : ISolutionItemSqlProvider<SmartScriptSolutionItem>
    {
        private readonly Lazy<ISmartScriptDatabaseProvider> database;
        private readonly IEventAggregator eventAggregator;
        private readonly Lazy<ISmartFactory> smartFactory;
        private readonly Lazy<ISmartDataManager> smartDataManager;
        private readonly Lazy<ISmartScriptExporter> exporter;
        private readonly Lazy<IEditorFeatures> editorFeatures;
        private readonly Lazy<ISmartScriptImporter> importer;

        public SmartScriptSqlGenerator(IEventAggregator eventAggregator,
            Lazy<ISmartScriptDatabaseProvider> database,
            Lazy<ISmartFactory> smartFactory,
            Lazy<ISmartDataManager> smartDataManager,
            Lazy<ISmartScriptExporter> exporter,
            Lazy<IEditorFeatures> editorFeatures,
            Lazy<ISmartScriptImporter> importer)
        {
            this.eventAggregator = eventAggregator;
            this.database = database;
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.exporter = exporter;
            this.editorFeatures = editorFeatures;
            this.importer = importer;
        }

        public async Task<IQuery> GenerateSql(SmartScriptSolutionItem item)
        {
            SmartScript script = new(item, smartFactory.Value, smartDataManager.Value, new EmptyMessageboxService(), editorFeatures.Value, importer.Value);
            var lines = (await database.Value.GetScriptFor(item.Entry ?? 0, item.EntryOrGuid, item.SmartType)).ToList();
            var conditions = (await database.Value.GetConditionsForScript(item.Entry, item.EntryOrGuid, item.SmartType)).ToList();
            await importer.Value.Import(script, true, lines, conditions, null);
            return await exporter.Value.GenerateSql(item, script);
        }

        private class EmptyMessageboxService : IMessageBoxService
        {
            public async Task<T?> ShowDialog<T>(IMessageBox<T> messageBox) => default;
        }
    }
}