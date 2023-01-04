using System;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.EventAiEditor;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor;
using WDE.EventAiEditor.Editor.UserControls;
using WDE.EventAiEditor.Models;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.MangosEventAiEditor.Providers
{
    [AutoRegisterToParentScope]
    public class EventAiSqlGenerator : ISolutionItemSqlProvider<EventAiSolutionItem>
    {
        private readonly Lazy<IEventAiDatabaseProvider> database;
        private readonly IEventAggregator eventAggregator;
        private readonly Lazy<IEventAiFactory> eventAiFactory;
        private readonly Lazy<IEventAiDataManager> eventAiDataManager;
        private readonly Lazy<IEventAiExporter> exporter;
        private readonly Lazy<IEventAiImporter> importer;

        public EventAiSqlGenerator(IEventAggregator eventAggregator,
            Lazy<IEventAiDatabaseProvider> database,
            Lazy<IEventAiFactory> eventAiFactory,
            Lazy<IEventAiDataManager> eventAiDataManager,
            Lazy<IEventAiExporter> exporter,
            Lazy<IEventAiImporter> importer)
        {
            this.eventAggregator = eventAggregator;
            this.database = database;
            this.eventAiFactory = eventAiFactory;
            this.eventAiDataManager = eventAiDataManager;
            this.exporter = exporter;
            this.importer = importer;
        }

        public async Task<IQuery> GenerateSql(EventAiSolutionItem item)
        {
            EventAiScript script = new(item, eventAiFactory.Value, eventAiDataManager.Value, new EmptyMessageboxService());
            var lines = (await database.Value.GetScriptFor(item.EntryOrGuid)).ToList();
            await importer.Value.Import(script, true, lines);
            return await exporter.Value.GenerateSql(item, script);
        }

        private class EmptyMessageboxService : IMessageBoxService
        {
            public async Task<T?> ShowDialog<T>(IMessageBox<T> messageBox) => default;
        }
    }
}