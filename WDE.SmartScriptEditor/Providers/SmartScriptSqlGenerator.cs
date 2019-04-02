using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Solution;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;
using WDE.Conditions.Data;
using WDE.Conditions.Model;

namespace WDE.SmartScriptEditor.Providers
{
    [AutoRegister]
    public class SmartScriptSqlGenerator : ISolutionItemSqlProvider<SmartScriptSolutionItem>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly Lazy<IDatabaseProvider> database;
        private readonly Lazy<ISmartFactory> smartFactory;
        private readonly Lazy<IConditionDataManager> conditionDataManager;

        public SmartScriptSqlGenerator(IEventAggregator eventAggregator, Lazy<IDatabaseProvider> database, Lazy<ISmartFactory> smartFactory, Lazy<IConditionDataManager> conditionDataManager)
        {
            this.eventAggregator = eventAggregator;
            this.database = database;
            this.smartFactory = smartFactory;
            this.conditionDataManager = conditionDataManager;
        }

        public string GenerateSql(SmartScriptSolutionItem item)
        {
            EventRequestGenerateSqlArgs args = new EventRequestGenerateSqlArgs();
            args.Item = item;

            eventAggregator.GetEvent<EventRequestGenerateSql>().Publish(args);

            if (args.Sql != null)
                return args.Sql;

            SmartScript script = new SmartScript(item, smartFactory.Value, conditionDataManager.Value);
            script.Load(database.Value.GetScriptFor(item.Entry, item.SmartType), database.Value.GetConditionsFor(Condition.CONDITION_SOURCE_SMART_EVENT, item.Entry, (int)item.SmartType));
            return new SmartScriptExporter(script, smartFactory.Value).GetSql();
        }
    }
}
