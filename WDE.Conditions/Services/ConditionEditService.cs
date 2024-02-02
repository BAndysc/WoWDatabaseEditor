using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Conditions.ViewModels;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Conditions.Services
{
    [AutoRegister]
    internal class ConditionEditService : IConditionEditService
    {
        private readonly IWindowManager windowManager;
        private readonly IContainerProvider containerProvider;
        private readonly IConditionQueryGenerator queryGenerator;
        private readonly IMySqlExecutor executor;
        private readonly IDatabaseProvider databaseProvider;

        public ConditionEditService(IWindowManager windowManager,
            IContainerProvider containerProvider,
            IConditionQueryGenerator queryGenerator,
            IMySqlExecutor executor,
            IDatabaseProvider databaseProvider)
        {
            this.windowManager = windowManager;
            this.containerProvider = containerProvider;
            this.queryGenerator = queryGenerator;
            this.executor = executor;
            this.databaseProvider = databaseProvider;
        }
    
        public async Task<IEnumerable<ICondition>?> EditConditions(IDatabaseProvider.ConditionKey conditionKey, IReadOnlyList<ICondition>? conditions, string? customTitle)
        {
            using var vm = containerProvider.Resolve<ConditionsEditorViewModel>(
                (typeof(IEnumerable<ICondition>), conditions ?? Enumerable.Empty<ICondition>()),
                (typeof(int), conditionKey.SourceType));
            vm.Title = customTitle ?? vm.Title;
            
            if (await windowManager.ShowDialog(vm))
            {
                return vm.GenerateConditions();
            }

            return null;
        }
        
        public async Task EditConditions(IDatabaseProvider.ConditionKeyMask conditionKeyMask, IDatabaseProvider.ConditionKey conditionKey, string? customTitle)
        {
            var conditions = await databaseProvider.GetConditionsForAsync(conditionKeyMask, conditionKey);
            var edited = await EditConditions(conditionKey.WithMask(conditionKeyMask), (IReadOnlyList<ICondition>)conditions, customTitle);
            if (edited == null)
                return;

            var newConditions = edited.Select(x => new AbstractConditionLine(conditionKey, x));
            
            var delete = queryGenerator.BuildDeleteQuery(conditionKey.WithMask(conditionKeyMask));
            var insert = queryGenerator.BuildInsertQuery(newConditions.ToList<IConditionLine>());
            var transaction = Queries.BeginTransaction(DataDatabaseType.World);
            transaction.Add(delete);
            transaction.Add(insert);
            await executor.ExecuteSql(transaction.Close());
        }

        public void OpenStandaloneConditions(IDatabaseProvider.ConditionKey conditionKey)
        {
            throw new System.NotImplementedException();
        }
    }
}