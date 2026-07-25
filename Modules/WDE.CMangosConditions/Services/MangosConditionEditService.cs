using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.CMangosConditions.Models;
using WDE.CMangosConditions.ViewModels;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.Services
{
    // requires IMangosDatabaseProvider, which only cmangos cores register;
    // other cores fall back to FallbackMangosConditionService
    [AutoRegister]
    [RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
    internal class MangosConditionEditService : IMangosConditionService
    {
        private readonly IWindowManager windowManager;
        private readonly IContainerProvider containerProvider;
        private readonly IMangosConditionQueryGenerator queryGenerator;
        private readonly IMySqlExecutor executor;
        private readonly IMangosDatabaseProvider databaseProvider;
        private readonly IIdGeneratorService idGenerator;

        public MangosConditionEditService(IWindowManager windowManager,
            IContainerProvider containerProvider,
            IMangosConditionQueryGenerator queryGenerator,
            IMySqlExecutor executor,
            IMangosDatabaseProvider databaseProvider,
            IIdGeneratorService idGenerator)
        {
            this.windowManager = windowManager;
            this.containerProvider = containerProvider;
            this.queryGenerator = queryGenerator;
            this.executor = executor;
            this.databaseProvider = databaseProvider;
            this.idGenerator = idGenerator;
        }

        public async Task<IReadOnlyList<IMangosConditionLine>?> EditConditions(IReadOnlyList<IMangosConditionLine> conditions, string? customTitle = null,
            MangosConditionSourceTarget? sourceTarget = null)
        {
            var result = await Edit(conditions, requireSingleRoot: false, customTitle, sourceTarget);
            return result?.Lines;
        }

        public async Task<MangosConditionTreeEditResult?> EditConditionTree(uint rootEntry,
            IReadOnlyList<IMangosConditionLine> knownConditions, uint firstFreeEntry, string? customTitle = null,
            MangosConditionSourceTarget? sourceTarget = null)
        {
            var closure = FilterClosure(rootEntry, knownConditions);

            var result = await Edit(closure, requireSingleRoot: true, customTitle, sourceTarget,
                firstFreeEntryProvider: _ => Task.FromResult(firstFreeEntry));
            if (result == null)
                return null;

            return new MangosConditionTreeEditResult(result.Lines,
                result.RootEntries.Count > 0 ? result.RootEntries[0] : 0);
        }

        public string BuildReadable(uint rootEntry, IReadOnlyList<IMangosConditionLine> knownConditions,
            MangosConditionSourceTarget? sourceTarget = null)
        {
            if (rootEntry == 0)
                return "";
            var closure = FilterClosure(rootEntry, knownConditions);
            var factory = containerProvider.Resolve<IMangosConditionsFactory>();
            var root = MangosConditionTreeCodec.BuildTree(closure, factory)
                .FirstOrDefault(r => r.OriginalEntry == rootEntry);
            if (root == null)
                return $"condition {rootEntry}";
            if (sourceTarget != null)
                foreach (var node in root.Descendants())
                    node.SetSourceTarget(sourceTarget);
            return RenderReadable(root, topLevel: true);
        }

        private static string RenderReadable(MangosConditionViewModel node, bool topLevel)
        {
            string Wrap(string text) => topLevel ? text : "(" + text + ")";
            switch (node.ConditionType)
            {
                case MangosConditionTreeCodec.TypeAnd:
                    return Wrap(string.Join(" AND ", node.Children.Select(c => RenderReadable(c, false))));
                case MangosConditionTreeCodec.TypeOr:
                    return Wrap(string.Join(" OR ", node.Children.Select(c => RenderReadable(c, false))));
                case MangosConditionTreeCodec.TypeNot:
                    return node.Children.Count == 1
                        ? "NOT (" + RenderReadable(node.Children[0], true) + ")"
                        : "NOT (?)";
                default:
                    return node.GetReadable(withTags: false, withEntry: false);
            }
        }

        /// <summary>rootEntry's row plus everything it transitively references, out of the passed rows.</summary>
        private static IReadOnlyList<IMangosConditionLine> FilterClosure(uint rootEntry, IReadOnlyList<IMangosConditionLine> known)
        {
            if (rootEntry == 0)
                return Array.Empty<IMangosConditionLine>();

            var byEntry = new Dictionary<uint, IMangosConditionLine>();
            foreach (var line in known)
                if (line.ConditionEntry != 0)
                    byEntry.TryAdd(line.ConditionEntry, line);

            var result = new List<IMangosConditionLine>();
            var seen = new HashSet<uint>();
            var queue = new Queue<uint>();
            queue.Enqueue(rootEntry);
            seen.Add(rootEntry);
            while (queue.Count > 0)
            {
                if (!byEntry.TryGetValue(queue.Dequeue(), out var line))
                    continue;
                result.Add(line);
                foreach (var reference in MangosConditionTreeCodec.ChildRefs(line))
                    if (seen.Add(reference))
                        queue.Enqueue(reference);
            }

            return result;
        }

        public Task<IReadOnlyList<IMangosConditionLine>> LoadConditionsClosure(uint rootEntry)
        {
            return MangosConditionClosureLoader.Load(databaseProvider, rootEntry);
        }

        public async Task<uint> GetFirstFreeConditionEntry(uint localMax)
        {
            return (uint)await idGenerator.GetNext(new MangosConditionEntryIdType { LocalMax = localMax });
        }

        public async Task<uint?> EditConditions(uint rootEntry, string? customTitle = null,
            MangosConditionSourceTarget? sourceTarget = null)
        {
            var loaded = await MangosConditionClosureLoader.Load(databaseProvider, rootEntry);

            var result = await Edit(loaded, requireSingleRoot: true, customTitle, sourceTarget,
                firstFreeEntryProvider: GetFirstFreeConditionEntry);
            if (result == null)
                return null;

            var affected = loaded.Select(l => l.ConditionEntry)
                .Concat(result.Lines.Select(l => l.ConditionEntry))
                .Distinct()
                .ToList();

            var transaction = Queries.BeginTransaction(DataDatabaseType.World);
            transaction.Add(queryGenerator.BuildDeleteQuery(affected));
            transaction.Add(queryGenerator.BuildInsertQuery(result.Lines));
            await executor.ExecuteSql(transaction.Close());

            return result.RootEntries.Count > 0 ? result.RootEntries[0] : 0;
        }

        private async Task<MangosConditionSerializeResult?> Edit(IReadOnlyList<IMangosConditionLine> conditions,
            bool requireSingleRoot, string? customTitle, MangosConditionSourceTarget? sourceTarget,
            Func<uint, Task<uint>>? firstFreeEntryProvider = null)
        {
            using var vm = containerProvider.Resolve<MangosConditionsEditorViewModel>(
                (typeof(IReadOnlyList<IMangosConditionLine>), conditions),
                (typeof(bool), requireSingleRoot));
            vm.Title = customTitle ?? vm.Title;
            vm.SetSourceTarget(sourceTarget);

            if (!await windowManager.ShowDialog(vm))
                return null;

            uint localMax = conditions.Count == 0 ? 0 : conditions.Max(c => c.ConditionEntry);
            uint firstFree = firstFreeEntryProvider != null
                ? await firstFreeEntryProvider(localMax)
                : localMax + 1;
            var result = vm.GenerateResult(firstFree);
            if (firstFreeEntryProvider != null && result.Lines.Count > 0)
            {
                // the serializer numbers new nodes sequentially above firstFree on its own -
                // tell the generator, so the next allocation starts above them
                uint maxAssigned = result.Lines.Max(l => l.ConditionEntry);
                if (maxAssigned >= firstFree)
                    idGenerator.MarkUsed(new MangosConditionEntryIdType(), firstFree, maxAssigned);
            }
            return result;
        }

    }
}
