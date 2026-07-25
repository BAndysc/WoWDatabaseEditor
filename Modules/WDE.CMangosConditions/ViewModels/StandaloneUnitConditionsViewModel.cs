using System;
using System.Collections.Generic;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WDE.CMangosConditions.ViewModels
{
    /// <summary>
    /// Standalone `unit_condition` editor window: load any row by its (signed) id,
    /// edit its clauses and save it back, or start a new row with a free negative id.
    /// </summary>
    [AutoRegister]
    internal class StandaloneUnitConditionsViewModel : ObservableBase, IWindowViewModel
    {
        private readonly IContainerProvider containerProvider;
        private readonly IMangosDatabaseProvider databaseProvider;
        private readonly IMangosUnitConditionQueryGenerator queryGenerator;
        private readonly IMySqlExecutor executor;

        private int? loadedId;

        public StandaloneUnitConditionsViewModel(IContainerProvider containerProvider,
            IMangosDatabaseProvider databaseProvider,
            IMangosUnitConditionQueryGenerator queryGenerator,
            IMySqlExecutor executor)
        {
            this.containerProvider = containerProvider;
            this.databaseProvider = databaseProvider;
            this.queryGenerator = queryGenerator;
            this.executor = executor;

            editor = CreateEditor(new AbstractMangosUnitConditionLine());

            LoadCommand = new AsyncAutoCommand(async () =>
            {
                if (!int.TryParse(IdText, out var id) || id == 0 || id == -1)
                {
                    Editor.Errors = "Enter a valid unit condition id to load (0 and -1 are not valid row ids).";
                    return;
                }

                var line = await databaseProvider.GetUnitConditionById(id);
                if (line == null)
                {
                    Editor.Errors = $"Unit condition {id} does not exist in the `unit_condition` table.";
                    return;
                }

                loadedId = id;
                ReplaceEditor(line);
            });

            NewCommand = new AsyncAutoCommand(async () =>
            {
                loadedId = null;
                var suggestedId = Math.Min(Math.Min(await databaseProvider.GetMinUnitConditionId(), 0), -1) - 1;
                IdText = suggestedId.ToString();
                ReplaceEditor(new AbstractMangosUnitConditionLine { Id = suggestedId });
            });

            SaveCommand = new AsyncAutoCommand(async () =>
            {
                Editor.Errors = string.Join("\n", Editor.Validate());
                if (Editor.Errors.Length > 0)
                    return;

                var line = Editor.ToLine();
                var affected = new List<int> { line.Id };
                if (loadedId.HasValue && loadedId.Value != line.Id)
                    affected.Add(loadedId.Value);

                var transaction = Queries.BeginTransaction(DataDatabaseType.World);
                transaction.Add(queryGenerator.BuildDeleteQuery(affected));
                transaction.Add(queryGenerator.BuildInsertQuery(new[] { line }));
                await executor.ExecuteSql(transaction.Close());

                loadedId = line.Id;
                IdText = line.Id.ToString();
            });
        }

        private UnitConditionEditorViewModel CreateEditor(IMangosUnitConditionLine line) =>
            containerProvider.Resolve<UnitConditionEditorViewModel>(
                (typeof(IMangosUnitConditionLine), line));

        private void ReplaceEditor(IMangosUnitConditionLine line)
        {
            var old = editor;
            editor = CreateEditor(line);
            RaisePropertyChanged(nameof(Editor));
            old.Dispose();
        }

        private UnitConditionEditorViewModel editor;
        public UnitConditionEditorViewModel Editor => editor;

        private string idText = "";
        public string IdText
        {
            get => idText;
            set => SetProperty(ref idText, value);
        }

        public AsyncAutoCommand LoadCommand { get; }
        public AsyncAutoCommand NewCommand { get; }
        public AsyncAutoCommand SaveCommand { get; }

        public int DesiredWidth => 800;
        public int DesiredHeight => 650;
        public string Title => "CMaNGOS unit conditions";
        public bool Resizeable => true;
        public ImageUri? Icon => new ImageUri("Icons/document_conditions.png");

        public override void Dispose()
        {
            base.Dispose();
            editor.Dispose();
        }
    }
}
