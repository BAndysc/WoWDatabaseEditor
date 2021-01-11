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

        public SmartScriptSqlGenerator(IEventAggregator eventAggregator,
            Lazy<IDatabaseProvider> database,
            Lazy<ISmartFactory> smartFactory)
        {
            this.eventAggregator = eventAggregator;
            this.database = database;
            this.smartFactory = smartFactory;
        }

        public string GenerateSql(SmartScriptSolutionItem item)
        {
            var args = new EventRequestGenerateSqlArgs();
            args.Item = item;

            eventAggregator.GetEvent<EventRequestGenerateSql>().Publish(args);

            if (args.Sql != null)
                return args.Sql;

            var script = new SmartScript(item, smartFactory.Value);
            script.Load(database.Value.GetScriptFor(item.Entry, item.SmartType));
            return new SmartScriptExporter(script, smartFactory.Value).GetSql();
        }
    }
}