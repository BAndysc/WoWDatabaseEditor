using System;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptSqlGenerator : ISolutionItemSqlProvider<SmartScriptSolutionItem>
    {
        private readonly Lazy<IDatabaseProvider> database;
        private readonly IEventAggregator eventAggregator;
        private readonly Lazy<ISmartFactory> smartFactory;
        private readonly Lazy<ISmartDataManager> smartDataManager;
        private readonly Lazy<ISmartScriptExporter> exporter;
        private readonly Lazy<ISmartScriptImporter> importer;

        public SmartScriptSqlGenerator(IEventAggregator eventAggregator,
            Lazy<IDatabaseProvider> database,
            Lazy<ISmartFactory> smartFactory,
            Lazy<ISmartDataManager> smartDataManager,
            Lazy<ISmartScriptExporter> exporter,
            Lazy<ISmartScriptImporter> importer)
        {
            this.eventAggregator = eventAggregator;
            this.database = database;
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
            this.exporter = exporter;
            this.importer = importer;
        }

        public async Task<string> GenerateSql(SmartScriptSolutionItem item)
        {
            SmartScript script = new(item, smartFactory.Value, smartDataManager.Value, new EmptyMessageboxService());
            var lines = database.Value.GetScriptFor(item.Entry, item.SmartType).ToList();
            var conditions = database.Value.GetConditionsFor(SmartConstants.ConditionSourceSmartScript, item.Entry, (int)item.SmartType).ToList();
            importer.Value.Import(script, lines, conditions, null);
            return exporter.Value.GenerateSql(script);
        }

        private class EmptyMessageboxService : IMessageBoxService
        {
            public Task<T?> ShowDialog<T>(IMessageBox<T> messageBox) => default;
        }
    }
}