using System;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Providers
{
    [AutoRegister]
    public class SmartScriptSqlGenerator : ISolutionItemSqlProvider<SmartScriptSolutionItem>
    {
        private readonly Lazy<IDatabaseProvider> database;
        private readonly IEventAggregator eventAggregator;
        private readonly Lazy<ISmartFactory> smartFactory;
        private readonly Lazy<ISmartDataManager> smartDataManager;

        public SmartScriptSqlGenerator(IEventAggregator eventAggregator,
            Lazy<IDatabaseProvider> database,
            Lazy<ISmartFactory> smartFactory,
            Lazy<ISmartDataManager> smartDataManager)
        {
            this.eventAggregator = eventAggregator;
            this.database = database;
            this.smartFactory = smartFactory;
            this.smartDataManager = smartDataManager;
        }

        public string GenerateSql(SmartScriptSolutionItem item)
        {
            EventRequestGenerateSqlArgs args = new();
            args.Item = item;

            eventAggregator.GetEvent<EventRequestGenerateSql>().Publish(args);

            if (args.Sql != null)
                return args.Sql;

            SmartScript script = new(item, smartFactory.Value);
            var lines = database.Value.GetScriptFor(item.Entry, item.SmartType);
            var conditions = database.Value.GetConditionsFor(SmartConstants.ConditionSourceSmartScript, item.Entry, (int)item.SmartType);
            script.Load(lines, conditions);
            return new SmartScriptExporter(script, smartFactory.Value, smartDataManager.Value).GetSql();
        }
    }
}