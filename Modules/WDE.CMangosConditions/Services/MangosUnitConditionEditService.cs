using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.Services
{
    // requires IMangosDatabaseProvider, which only cmangos cores register;
    // other cores fall back to FallbackMangosUnitConditionService
    [AutoRegister]
    [RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
    internal class MangosUnitConditionEditService : IMangosUnitConditionService
    {
        private readonly IWindowManager windowManager;
        private readonly IContainerProvider containerProvider;
        private readonly IMangosUnitConditionQueryGenerator queryGenerator;
        private readonly IMySqlExecutor executor;
        private readonly IMangosDatabaseProvider databaseProvider;
        private readonly IUnitConditionClauseFactory clauseFactory;

        public MangosUnitConditionEditService(IWindowManager windowManager,
            IContainerProvider containerProvider,
            IMangosUnitConditionQueryGenerator queryGenerator,
            IMySqlExecutor executor,
            IMangosDatabaseProvider databaseProvider,
            IUnitConditionClauseFactory clauseFactory)
        {
            this.windowManager = windowManager;
            this.containerProvider = containerProvider;
            this.queryGenerator = queryGenerator;
            this.executor = executor;
            this.databaseProvider = databaseProvider;
            this.clauseFactory = clauseFactory;
        }

        public async Task<int?> EditUnitCondition(int? id, string? customTitle = null)
        {
            IMangosUnitConditionLine? loaded = id.HasValue ? await LoadUnitCondition(id.Value) : null;
            var line = loaded ?? new AbstractMangosUnitConditionLine
            {
                Id = id ?? await GetFreeUnitConditionId()
            };

            var edited = await Edit(line, customTitle);
            if (edited == null)
                return null;

            var affected = new List<int> { edited.Id };
            if (loaded != null && loaded.Id != edited.Id)
                affected.Add(loaded.Id);

            var transaction = Queries.BeginTransaction(DataDatabaseType.World);
            transaction.Add(queryGenerator.BuildDeleteQuery(affected));
            transaction.Add(queryGenerator.BuildInsertQuery(new[] { edited }));
            await executor.ExecuteSql(transaction.Close());

            return edited.Id;
        }

        public async Task<IMangosUnitConditionLine?> EditUnitConditionInMemory(IMangosUnitConditionLine? line,
            int suggestedNewId, string? customTitle = null)
        {
            var toEdit = line ?? new AbstractMangosUnitConditionLine { Id = suggestedNewId };
            return await Edit(toEdit, customTitle);
        }

        public string BuildReadable(IMangosUnitConditionLine line)
        {
            var readables = new List<string>();
            for (int i = 0; i < IMangosUnitConditionLine.ClausesCount; ++i)
            {
                var clause = line.GetClause(i);
                if (!clause.IsNone)
                    readables.Add(clauseFactory.Create(clause).Readable);
            }
            if (readables.Count == 0)
                return "always true";
            var separator = (line.Flags & 1) != 0 ? " OR " : " AND ";
            return string.Join(separator, readables);
        }

        public Task<IMangosUnitConditionLine?> LoadUnitCondition(int id)
        {
            if (id == 0 || id == -1)
                return Task.FromResult<IMangosUnitConditionLine?>(null);
            return databaseProvider.GetUnitConditionById(id);
        }

        public async Task<int> GetFreeUnitConditionId(int localMin = 0)
        {
            var dbMin = await databaseProvider.GetMinUnitConditionId();
            return Math.Min(Math.Min(dbMin, localMin), -1) - 1;
        }

        private async Task<AbstractMangosUnitConditionLine?> Edit(IMangosUnitConditionLine line, string? customTitle)
        {
            using var vm = containerProvider.Resolve<UnitConditionEditorViewModel>(
                (typeof(IMangosUnitConditionLine), line));
            vm.Title = customTitle ?? vm.Title;

            if (!await windowManager.ShowDialog(vm))
                return null;

            return vm.ToLine();
        }
    }
}
