using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.CMangosConditions.Models;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.ViewModels
{
    /// <summary>
    /// Standalone `conditions` editor window: load any condition tree by its root
    /// condition_entry, edit it and save it back (or start from scratch and save new rows).
    /// </summary>
    [AutoRegister]
    internal class StandaloneMangosConditionsViewModel : ObservableBase, IWindowViewModel
    {
        private readonly IContainerProvider containerProvider;
        private readonly IMangosDatabaseProvider databaseProvider;
        private readonly IMangosConditionQueryGenerator queryGenerator;
        private readonly IMySqlExecutor executor;
        private readonly IIdGeneratorService idGenerator;

        private HashSet<uint> loadedEntries = new();

        public StandaloneMangosConditionsViewModel(IContainerProvider containerProvider,
            IMangosDatabaseProvider databaseProvider,
            IMangosConditionQueryGenerator queryGenerator,
            IMySqlExecutor executor,
            IIdGeneratorService idGenerator)
        {
            this.containerProvider = containerProvider;
            this.databaseProvider = databaseProvider;
            this.queryGenerator = queryGenerator;
            this.executor = executor;
            this.idGenerator = idGenerator;

            editor = CreateEditor(Array.Empty<IMangosConditionLine>());

            LoadCommand = new AsyncAutoCommand(async () =>
            {
                if (!uint.TryParse(RootEntryText, out var rootEntry) || rootEntry == 0)
                {
                    Editor.Errors = "Enter a valid root condition entry to load.";
                    return;
                }

                var lines = await MangosConditionClosureLoader.Load(databaseProvider, rootEntry);
                if (lines.Count == 0)
                {
                    Editor.Errors = $"Condition {rootEntry} does not exist in the `conditions` table.";
                    return;
                }

                loadedEntries = lines.Select(l => l.ConditionEntry).ToHashSet();
                ReplaceEditor(lines);
            });

            NewCommand = new AsyncAutoCommand(async () =>
            {
                loadedEntries.Clear();
                RootEntryText = "";
                ReplaceEditor(Array.Empty<IMangosConditionLine>());
                await Task.CompletedTask;
            });

            SaveCommand = new AsyncAutoCommand(async () =>
            {
                var errors = MangosConditionTreeCodec.Validate(Editor.Roots);
                if (Editor.Roots.Count == 0)
                    errors.Insert(0, "Nothing to save - add a condition first.");
                Editor.Errors = string.Join("\n", errors);
                if (Editor.Errors.Length > 0)
                    return;

                var localMax = loadedEntries.Count == 0 ? 0 : loadedEntries.Max();
                var firstFree = (uint)await idGenerator.GetNext(new MangosConditionEntryIdType { LocalMax = localMax });
                var result = Editor.GenerateResult(firstFree);
                var maxAssigned = result.Lines.Count == 0 ? 0 : result.Lines.Max(l => l.ConditionEntry);
                if (maxAssigned >= firstFree)
                    idGenerator.MarkUsed(new MangosConditionEntryIdType(), firstFree, maxAssigned);

                var affected = loadedEntries
                    .Concat(result.Lines.Select(l => l.ConditionEntry))
                    .Distinct()
                    .ToList();

                var transaction = Queries.BeginTransaction(DataDatabaseType.World);
                transaction.Add(queryGenerator.BuildDeleteQuery(affected));
                transaction.Add(queryGenerator.BuildInsertQuery(result.Lines));
                await executor.ExecuteSql(transaction.Close());

                // reload from what was written, so every node shows its real entry
                loadedEntries = result.Lines.Select(l => l.ConditionEntry).ToHashSet();
                RootEntryText = result.RootEntries.Count > 0 ? result.RootEntries[0].ToString() : "";
                ReplaceEditor(result.Lines.ToList<IMangosConditionLine>());
            });
        }

        private MangosConditionsEditorViewModel CreateEditor(IReadOnlyList<IMangosConditionLine> lines) =>
            containerProvider.Resolve<MangosConditionsEditorViewModel>(
                (typeof(IReadOnlyList<IMangosConditionLine>), lines),
                (typeof(bool), false));

        private void ReplaceEditor(IReadOnlyList<IMangosConditionLine> lines)
        {
            var old = editor;
            editor = CreateEditor(lines);
            RaisePropertyChanged(nameof(Editor));
            old.Dispose();
        }

        private MangosConditionsEditorViewModel editor;
        public MangosConditionsEditorViewModel Editor => editor;

        private string rootEntryText = "";
        public string RootEntryText
        {
            get => rootEntryText;
            set => SetProperty(ref rootEntryText, value);
        }

        public AsyncAutoCommand LoadCommand { get; }
        public AsyncAutoCommand NewCommand { get; }
        public AsyncAutoCommand SaveCommand { get; }

        public int DesiredWidth => 900;
        public int DesiredHeight => 700;
        public string Title => "CMaNGOS conditions";
        public bool Resizeable => true;
        public ImageUri? Icon => new ImageUri("Icons/document_conditions.png");

        public override void Dispose()
        {
            base.Dispose();
            editor.Dispose();
        }
    }
}
