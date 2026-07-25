using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.IdGenerator;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Editor.ViewModels.Editing;
using WDE.DbScriptsEditor.Exporter;
using WDE.DbScriptsEditor.History;
using WDE.DbScriptsEditor.Models;
using WDE.DbScriptsEditor.Providers;
using WDE.DbScriptsEditor.Services;
using WDE.DbScriptsEditor.Settings;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.SqlQueryGenerator;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // The dbscript document editor. The document is a flat list of ROWS — action, wait and
    // comment are EQUAL, independent entities: selectable together, reorderable independently,
    // copyable, deletable. They are squashed into physical dbscript lines (delay column, comment
    // column) only at save; loading derives them back.
    public class DbScriptEditorViewModel : ObservableBase, ISolutionItemDocument
    {
        private readonly DbScriptSolutionItem item;
        private readonly IDbScriptDatabaseProvider databaseProvider;
        private readonly IDbScriptDataManager dataManager;
        private readonly IParameterFactory parameterFactory;
        private readonly IParameterPickerService parameterPickerService;
        private readonly IStatusBar statusbar;
        private readonly IMessageBoxService messageBoxService;
        private readonly ITaskRunner taskRunner;
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly IRemoteConnectorService remoteConnectorService;
        private readonly IDbScriptExporter exporter;
        private readonly IEventAggregator eventAggregator;
        private readonly ITableEditorPickerService tableEditorPickerService;
        private readonly IClipboardService clipboardService;
        private readonly IWindowManager windowManager;
        private readonly IInputBoxService inputBoxService;
        private readonly IMangosConditionService conditionService;
        private readonly IMangosConditionQueryGenerator conditionQueryGenerator;
        private readonly IIdGeneratorService idGenerator;
        private readonly IFavouriteDbScriptCommandsService favouriteCommands;
        private readonly IDbScriptEditorSettings editorSettings;
        private readonly IHistoryManager history;

        private EditableDbScript? script;
        private bool isLoading = true;
        private readonly DbScriptConditionsStore conditionsStore = new();
        // readable depends on the condition AND on the actors of the steps the if row guards
        private readonly Dictionary<(long conditionId, string target, string? source), string> conditionReadableCache = new();

        public DbScriptEditorViewModel(
            DbScriptSolutionItem item,
            IHistoryManager history,
            IDbScriptDatabaseProvider databaseProvider,
            IDbScriptDataManager dataManager,
            IParameterFactory parameterFactory,
            IParameterPickerService parameterPickerService,
            IStatusBar statusbar,
            IMessageBoxService messageBoxService,
            ITaskRunner taskRunner,
            IMySqlExecutor mySqlExecutor,
            IRemoteConnectorService remoteConnectorService,
            IDbScriptExporter exporter,
            IEventAggregator eventAggregator,
            ITableEditorPickerService tableEditorPickerService,
            IClipboardService clipboardService,
            IWindowManager windowManager,
            IInputBoxService inputBoxService,
            IMangosConditionService conditionService,
            IMangosConditionQueryGenerator conditionQueryGenerator,
            IIdGeneratorService idGenerator,
            IFavouriteDbScriptCommandsService favouriteCommands,
            IDbScriptEditorSettings editorSettings)
        {
            this.idGenerator = idGenerator;
            this.favouriteCommands = favouriteCommands;
            this.editorSettings = editorSettings;
            this.item = item;
            this.history = history;
            this.databaseProvider = databaseProvider;
            this.dataManager = dataManager;
            this.parameterFactory = parameterFactory;
            this.parameterPickerService = parameterPickerService;
            this.statusbar = statusbar;
            this.messageBoxService = messageBoxService;
            this.taskRunner = taskRunner;
            this.mySqlExecutor = mySqlExecutor;
            this.remoteConnectorService = remoteConnectorService;
            this.exporter = exporter;
            this.eventAggregator = eventAggregator;
            this.tableEditorPickerService = tableEditorPickerService;
            this.clipboardService = clipboardService;
            this.windowManager = windowManager;
            this.inputBoxService = inputBoxService;
            this.conditionService = conditionService;
            this.conditionQueryGenerator = conditionQueryGenerator;

            var info = DbScriptTypes.GetInfo(item.ScriptType);
            Title = DbScriptNameProvider.BuildName(info, item.ScriptId, parameterFactory);
            SourceLabel = info.SourceLabel;
            TargetLabel = info.TargetLabel;

            UndoCommand = new DelegateCommand(history.Undo, () => history.CanUndo);
            RedoCommand = new DelegateCommand(history.Redo, () => history.CanRedo);
            history.PropertyChanged += (_, _) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };

            SaveCommand = new AsyncAutoCommand(() => taskRunner.ScheduleTask("Save dbscript to database", SaveToDb));
            Copy = new AsyncAutoCommand(CopySelected, () => SelectedRow != null);
            Cut = new AsyncAutoCommand(async () => { await CopySelected(); DeleteSelectedRows(); }, () => SelectedRow != null);
            Paste = new AsyncAutoCommand(PasteRows);

            AddStep = new AsyncAutoCommand(AddStepCommand);
            ChangeCommand = new AsyncAutoCommand<DbScriptStepViewModel>(vm => vm != null ? ChangeStepCommand(vm.Step) : Task.CompletedTask);
            EditParameter = new AsyncAutoCommand<DbScriptEditableParameter>(EditParameterCommand);
            EditContextLink = new AsyncAutoCommand<object>(EditContextLinkCommand);
            EditAction = new AsyncAutoCommand<DbScriptStepViewModel>(vm => vm != null ? EditActionCommand(vm.Step) : Task.CompletedTask);
            DeleteSelected = new DelegateCommand(DeleteSelectedRows, () => SelectedRow != null);
            DuplicateSelected = new DelegateCommand(DuplicateSelectedRows, () => SelectedRow != null);
            MoveSelectedUp = new DelegateCommand(() => MoveSelectedRows(-1), () => SelectedRow != null);
            MoveSelectedDown = new DelegateCommand(() => MoveSelectedRows(+1), () => SelectedRow != null);
            DeselectAll = new DelegateCommand(DeselectAllRows);
            RangeSelectTo = new DelegateCommand<DbScriptRowViewModel>(RangeSelectToRow);
            EditWait = new AsyncAutoCommand<DbScriptWaitViewModel>(EditWaitCommand);
            EditComment = new AsyncAutoCommand<DbScriptCommentViewModel>(EditCommentCommand);
            EditSelected = new AsyncAutoCommand(EditSelectedCommand, () => SelectedRow != null);
            // On an if row (or a row inside its block): edit that condition tree. On anything
            // else: wrap the selection in a brand-new if.
            EditSelectedCondition = new AsyncAutoCommand(EditSelectedConditionCommand, () => SelectedRow != null);
            EditIf = new AsyncAutoCommand<DbScriptIfViewModel>(vm =>
                vm != null ? EditConditionCommand(vm.If) : Task.CompletedTask);
            RemoveSelectedCondition = new DelegateCommand(() =>
            {
                // deletes the if row, keeps its member rows; the condition rows stay in the DB
                var ifRow = ResolveSelectedIfRow();
                if (script == null || ifRow == null)
                    return;
                using (script.BulkEdit("Remove if"))
                {
                    script.Rows.Remove(ifRow);
                    script.NormalizeIfMembership();
                }
            }, () => ResolveSelectedIfRow() != null);
            AddWait = new AsyncAutoCommand(() => InsertWaitRow());
            AddComment = new AsyncAutoCommand(() => InsertCommentRow());

            TaskRun().ListenErrors();
        }

        // The bound list: one VM per model row, mirrored 1:1 (same order, same indices).
        // If rows are model rows like any other, so no extra presentation layer is needed.
        public ObservableCollection<DbScriptRowViewModel> Rows { get; } = new();

        // The primary/most-recent selection. It does NOT own the other rows' selection state —
        // rows keep their own IsSelected, so Ctrl-multiselect works; a plain click clears the rest
        // via DeselectAllRequest from the row control.
        private DbScriptRowViewModel? selectedRow;
        public DbScriptRowViewModel? SelectedRow
        {
            get => selectedRow;
            set
            {
                if (SetProperty(ref selectedRow, value))
                {
                    DeleteSelected.RaiseCanExecuteChanged();
                    DuplicateSelected.RaiseCanExecuteChanged();
                    MoveSelectedUp.RaiseCanExecuteChanged();
                    MoveSelectedDown.RaiseCanExecuteChanged();
                    EditSelected.RaiseCanExecuteChanged();
                    EditSelectedCondition.RaiseCanExecuteChanged();
                    RemoveSelectedCondition.RaiseCanExecuteChanged();
                    (Copy as AsyncAutoCommand)?.RaiseCanExecuteChanged();
                    (Cut as AsyncAutoCommand)?.RaiseCanExecuteChanged();
                    RaisePropertyChanged(nameof(HasSelection));
                }
            }
        }

        public bool HasSelection => selectedRow != null;

        // All selected rows, in visual (row) order.
        public IEnumerable<DbScriptRowViewModel> SelectedRows => Rows.Where(r => r.IsSelected);

        public string SourceLabel { get; }
        public string TargetLabel { get; }

        public AsyncAutoCommand AddStep { get; }
        public AsyncAutoCommand<DbScriptStepViewModel> ChangeCommand { get; }
        // Double-click an action row → full "edit action" dialog (SmartScript-style).
        public AsyncAutoCommand<DbScriptStepViewModel> EditAction { get; }
        public AsyncAutoCommand<DbScriptEditableParameter> EditParameter { get; }
        // Clicking a link in the readable: dispatches on the context object type (a parameter value
        // picker, or the high-level source/target actor picker).
        public AsyncAutoCommand<object> EditContextLink { get; }
        public DelegateCommand DeleteSelected { get; }
        public DelegateCommand DuplicateSelected { get; }
        public DelegateCommand MoveSelectedUp { get; }
        public DelegateCommand MoveSelectedDown { get; }
        public DelegateCommand DeselectAll { get; }
        public DelegateCommand<DbScriptRowViewModel> RangeSelectTo { get; }
        public AsyncAutoCommand<DbScriptWaitViewModel> EditWait { get; }
        public AsyncAutoCommand<DbScriptCommentViewModel> EditComment { get; }
        // Context-menu / keyboard entry points.
        public AsyncAutoCommand EditSelected { get; }
        // Condition of the selected action: open the cmangos condition tree editor / unlink.
        public AsyncAutoCommand EditSelectedCondition { get; }
        public DelegateCommand RemoveSelectedCondition { get; }
        // Double-click an "if" row → condition tree editor.
        public AsyncAutoCommand<DbScriptIfViewModel> EditIf { get; }
        public AsyncAutoCommand AddWait { get; }
        public AsyncAutoCommand AddComment { get; }

        public DelegateCommand UndoCommand { get; }
        public DelegateCommand RedoCommand { get; }
        public IAsyncCommand SaveCommand { get; }

        private async Task TaskRun()
        {
            try
            {
                var lines = await databaseProvider.GetScript(item.ScriptType, item.ScriptId);
                script = new EditableDbScript(item.ScriptType, item.ScriptId, dataManager, parameterFactory);
                script.Rows.CollectionChanged += OnModelRowsChanged;
                script.DerivedChanged += RefreshIndentation; // InIf / if-condition changes re-indent
                script.DerivedChanged += RefreshConditionReadables; // block membership feeds if-row actor names
                script.Load(lines);
                RunInspections();
                history.AddHandler(new DbScriptHistoryHandler(script));

                // load the conditions the script references (closures of every condition_id)
                await conditionsStore.LoadClosuresAsync(
                    script.Steps.Select(s => (uint)s.ConditionId.Value),
                    databaseProvider.GetConditions);
                conditionReadableCache.Clear(); // rows rendered before the closures loaded cached the raw fallback
                RefreshConditionReadables();
            }
            catch (Exception e)
            {
                WDE.Common.LOG.LogError(e.ToString());
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Mirrors the model rows 1:1 into VM rows.
        private void OnModelRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var index = e.NewStartingIndex;
                    foreach (DbScriptRow row in e.NewItems!)
                        Rows.Insert(index++, CreateRowViewModel(row));
                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    for (var i = 0; i < e.OldItems!.Count; ++i)
                        RemoveRowViewModelAt(e.OldStartingIndex);
                    break;
                }
                case NotifyCollectionChangedAction.Move:
                    Rows.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                default:
                {
                    while (Rows.Count > 0)
                        RemoveRowViewModelAt(Rows.Count - 1);
                    if (script != null)
                        foreach (var row in script.Rows)
                            Rows.Add(CreateRowViewModel(row));
                    break;
                }
            }
            RefreshIndentation();
            RunInspections();
        }

        private DbScriptRowViewModel CreateRowViewModel(DbScriptRow row)
        {
            DbScriptRowViewModel vm = row switch
            {
                EditableDbScriptStep step => new DbScriptStepViewModel(step, PickBuddyEntry),
                DbScriptWaitRow wait => new DbScriptWaitViewModel(wait),
                DbScriptCommentRow comment => new DbScriptCommentViewModel(comment),
                DbScriptIfRow ifRow => new DbScriptIfViewModel(ifRow, GetConditionReadable),
                _ => throw new ArgumentOutOfRangeException(nameof(row)),
            };
            vm.PropertyChanged += OnRowViewModelPropertyChanged;
            if (vm is DbScriptStepViewModel stepVm)
                stepVm.Step.PropertyChanged += OnStepModelPropertyChanged;
            return vm;
        }

        private void RemoveRowViewModelAt(int index)
        {
            var removed = Rows[index];
            removed.PropertyChanged -= OnRowViewModelPropertyChanged;
            if (removed is DbScriptStepViewModel stepVm)
                stepVm.Step.PropertyChanged -= OnStepModelPropertyChanged;
            if (ReferenceEquals(SelectedRow, removed))
                SelectedRow = null;
            removed.Dispose();
            Rows.RemoveAt(index);
        }

        // The rows own their selection (EventAI-style). When a row becomes selected we make it
        // the SelectedRow so keyboard/context-menu commands track it.
        private bool suppressSelectionSync;
        private void OnRowViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (suppressSelectionSync || e.PropertyName != nameof(DbScriptRowViewModel.IsSelected))
                return;
            if (sender is DbScriptRowViewModel { IsSelected: true } vm)
            {
                SelectedRow = vm;
                anchorRow = vm; // a plain / ctrl selection (re)sets the range anchor
            }
            else if (ReferenceEquals(sender, SelectedRow))
                SelectedRow = SelectedRows.FirstOrDefault(); // fall back to any remaining selection
        }

        // Shift-click: select the contiguous range (of ALL row kinds) from the anchor to the
        // clicked row.
        private DbScriptRowViewModel? anchorRow;
        private void RangeSelectToRow(DbScriptRowViewModel? target)
        {
            if (target == null)
                return;
            var anchor = anchorRow ?? SelectedRow ?? target;
            var a = Rows.IndexOf(anchor);
            var b = Rows.IndexOf(target);
            if (a < 0 || b < 0)
                return;
            var (lo, hi) = a <= b ? (a, b) : (b, a);

            suppressSelectionSync = true;
            for (var i = 0; i < Rows.Count; i++)
                Rows[i].IsSelected = i >= lo && i <= hi;
            suppressSelectionSync = false;
            SelectedRow = target; // representative; anchor stays put
        }

        private void DeselectAllRows()
        {
            suppressSelectionSync = true;
            foreach (var vm in Rows)
                vm.IsSelected = false;
            suppressSelectionSync = false;
            SelectedRow = null;
        }

        // Any step recompute (parameter value, command or structural flag change) re-raises the
        // readable, which is our universal "something changed" signal to re-run inspections.
        private void OnStepModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(EditableDbScriptStep.FormattedReadable))
            {
                RunInspections();
                // a step's source/target (data_flags/buddy) feeds the actor names shown in the
                // readable of the if row guarding it
                RefreshConditionReadables();
            }
        }

        // ---- editing rows ----

        private async Task EditSelectedCommand()
        {
            switch (SelectedRow)
            {
                case DbScriptStepViewModel step:
                    await EditActionCommand(step.Step);
                    break;
                case DbScriptWaitViewModel wait:
                    await EditWaitCommand(wait);
                    break;
                case DbScriptCommentViewModel comment:
                    await EditCommentCommand(comment);
                    break;
                case DbScriptIfViewModel ifVm:
                    await EditConditionCommand(ifVm.If);
                    break;
            }
        }

        private async Task EditWaitCommand(DbScriptWaitViewModel? wait)
        {
            if (wait == null)
                return;
            var (duration, ok) = await PickNumber("Wait (ms)", wait.Wait.Duration.Value);
            if (!ok || duration <= 0)
                return;
            wait.Wait.Duration.Value = duration;
        }

        private async Task EditCommentCommand(DbScriptCommentViewModel? comment)
        {
            if (script == null || comment == null)
                return;
            var text = await inputBoxService.GetString("Comment", "Comment text",
                comment.Comment.Text.Value, multiline: true, allowEmpty: true);
            if (text == null)
                return;
            if (string.IsNullOrWhiteSpace(text))
                script.Rows.Remove(comment.Comment); // an empty comment row disappears
            else
                comment.Comment.Text.Value = text.Trim();
        }

        // ---- adding rows ----

        // Where a new row goes: right after the (primary) selected row, else at the end.
        private int InsertionIndex()
        {
            if (script == null)
                return 0;
            if (SelectedRow != null)
            {
                var idx = script.Rows.IndexOf(SelectedRow.Row);
                if (idx >= 0)
                    return idx + 1;
            }
            return script.Rows.Count;
        }

        private void SelectOnly(DbScriptRow modelRow)
        {
            DeselectAllRows();
            var vm = Rows.FirstOrDefault(r => ReferenceEquals(r.Row, modelRow));
            if (vm != null)
                vm.IsSelected = true;
        }

        // New rows inserted after the selection inherit its block membership: adding onto a
        // selected if row or one of its members puts the new row inside that block.
        private bool InsideAtInsertion() =>
            SelectedRow is DbScriptIfViewModel || (SelectedRow?.InConditionBlock ?? false);

        private async Task InsertWaitRow()
        {
            if (script == null)
                return;
            var (duration, ok) = await PickNumber("Wait (ms)", 1000);
            if (!ok || duration <= 0)
                return;
            var wait = script.MakeWait(duration);
            wait.InIf = InsideAtInsertion();
            script.Rows.Insert(InsertionIndex(), wait);
            SelectOnly(wait);
        }

        private async Task InsertCommentRow()
        {
            if (script == null)
                return;
            var text = await inputBoxService.GetString("Comment", "Comment text", "", multiline: true, allowEmpty: false);
            if (string.IsNullOrWhiteSpace(text))
                return;
            var comment = script.MakeComment(text.Trim());
            comment.InIf = InsideAtInsertion();
            script.Rows.Insert(InsertionIndex(), comment);
            SelectOnly(comment);
        }

        private Task AddStepCommand() =>
            editorSettings.AddFlow == DbScriptAddFlow.ActionFirst ? AddStepActionFirst() : AddStepWizard();

        // Action-first adding (opt-in via settings): pick just the command, then the parameters
        // dialog opens directly — source/target are changed there. Insert happens on Accept.
        private async Task AddStepActionFirst()
        {
            if (script == null)
                return;

            using var dialog = new DbScriptSelectViewModel("Add action", null, dataManager, favouriteCommands,
                includeStructural: true);
            if (!await windowManager.ShowDialog(dialog) || dialog.SelectedItem == null)
                return;
            var key = dialog.SelectedItem.Key;
            if (key == DbScriptSelectViewModel.WaitKey)
            {
                await InsertWaitRow();
                return;
            }
            if (key == DbScriptSelectViewModel.CommentKey)
            {
                await InsertCommentRow();
                return;
            }
            var (commandId, variant) = DecodeCommandKey(key);

            var draft = new EditableDbScriptStep(new AbstractDbScriptLine { Id = script.ScriptId, Command = commandId },
                dataManager, script.TypeInfo, parameterFactory);
            if (variant != null)
                draft.ApplyVariantPresets(variant);
            draft.ApplyParameterDefaults();

            await ShowStepDialog(draft, () => InsertDraft(draft));
        }

        // SmartScript-wizard adding (the default): pick WHO acts (source) → a command compatible
        // with that source → WHO is acted upon (only when the command uses a target, and only
        // choices the command's target types accept) → the parameters dialog. The step lands in
        // the document only when the dialog is accepted; cancelling any stage aborts the whole
        // add. Wait/Comment stay reachable as structural entries of the first picker.
        private async Task AddStepWizard()
        {
            if (script == null)
                return;
            var info = script.TypeInfo;

            // 1. source
            var sourceItems = BuildActorItems(null, info, isSource: true, includeStructural: true);
            var sourceKey = await PickActorKey("Add action — pick source (who acts)", sourceItems, null);
            if (!sourceKey.HasValue)
                return;
            if (sourceKey.Value == DbScriptSelectViewModel.WaitKey)
            {
                await InsertWaitRow();
                return;
            }
            if (sourceKey.Value == DbScriptSelectViewModel.CommentKey)
            {
                await InsertCommentRow();
                return;
            }

            // gather the source's locator values right away (entry/guid/pool follow-up prompts)
            var sourceChoice = await ResolveActorChoice(sourceKey.Value, BuddyDescriptor.None);
            if (sourceChoice == null)
                return;

            // 2. command, filtered to what the picked source can execute (and what has any
            // pickable target in this script type)
            using var commandDialog = new DbScriptSelectViewModel("Pick action", null, dataManager, favouriteCommands,
                def => SourceCompatibleWithCommand(sourceKey.Value, def, info) && CommandTargetSatisfiable(def, sourceKey.Value, info));
            if (!await windowManager.ShowDialog(commandDialog) || commandDialog.SelectedItem == null)
                return;
            var (commandId, variant) = DecodeCommandKey(commandDialog.SelectedItem.Key);
            var def = dataManager.TryGetCommand(commandId);

            // 3. target (only when the command uses one)
            long? targetKey = null;
            if (def != null && def.UsesTarget)
            {
                var targetItems = BuildActorItems(def, info, isSource: false, includeStructural: false, buddySourceKey: sourceKey);
                targetKey = await PickActorKey("Pick target (acted upon)", targetItems, null);
                if (!targetKey.HasValue)
                    return;
            }

            // 4. build the draft step and compile the picked actors into flags + buddy
            var draft = new EditableDbScriptStep(new AbstractDbScriptLine { Id = script.ScriptId, Command = commandId },
                dataManager, info, parameterFactory);
            if (variant != null)
                draft.ApplyVariantPresets(variant);
            draft.ApplyParameterDefaults();

            var flags = draft.DecodeFlags();
            flags = DbScriptSourceTargetCompiler.SetSlot(flags, true, sourceChoice.Value.kind, sourceChoice.Value.buddy);
            if (targetKey.HasValue)
            {
                if (targetKey.Value == ActorSameBuddy)
                {
                    flags = DbScriptSourceTargetCompiler.SetSlot(flags, false, SourceTargetKind.Buddy, flags.Buddy);
                }
                else
                {
                    var targetChoice = await ResolveActorChoice(targetKey.Value, flags.Buddy);
                    if (targetChoice == null)
                        return;
                    flags = DbScriptSourceTargetCompiler.SetSlot(flags, false, targetChoice.Value.kind, targetChoice.Value.buddy);
                }
            }
            draft.ApplyDecodedFlags(flags);

            // 5. parameters dialog; insert on accept
            await ShowStepDialog(draft, () => InsertDraft(draft));
        }

        // Materializes an accepted draft into the document (placement per current selection).
        private void InsertDraft(EditableDbScriptStep draft)
        {
            var step = script!.MakeStep(draft.ToLine());
            step.InIf = InsideAtInsertion();
            script.Rows.Insert(InsertionIndex(), step);
            SelectOnly(step);
        }

        // ---- delete / duplicate / move / copy / paste ----

        private void DeleteSelectedRows()
        {
            if (script == null)
                return;
            var targets = SelectedRows.ToList();
            if (targets.Count == 0)
                return;
            using (script.BulkEdit("Delete rows"))
            {
                foreach (var vm in targets)
                    script.Rows.Remove(vm.Row);
                // deleting an if row frees its members (their stale flags get cleared)
                script.NormalizeIfMembership();
            }
        }

        private DbScriptRow CloneRow(DbScriptRow row)
        {
            DbScriptRow clone = row switch
            {
                EditableDbScriptStep step => script!.MakeStep(step.ToLine()),
                DbScriptWaitRow wait => script!.MakeWait(wait.Duration.Value),
                DbScriptCommentRow comment => script!.MakeComment(comment.Text.Value),
                DbScriptIfRow ifRow => script!.MakeIf(ifRow.ConditionId.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(row)),
            };
            clone.InIf = row.InIf;
            return clone;
        }

        private void DuplicateSelectedRows()
        {
            if (script == null)
                return;
            var targets = SelectedRows.ToList();
            if (targets.Count == 0)
                return;
            using (script.BulkEdit("Duplicate rows"))
            {
                // Insert each clone right after its original; process high-to-low so indices hold.
                foreach (var vm in targets.OrderByDescending(v => script.Rows.IndexOf(v.Row)))
                {
                    var idx = script.Rows.IndexOf(vm.Row);
                    if (idx < 0)
                        continue;
                    script.Rows.Insert(idx + 1, CloneRow(vm.Row));
                }
            }
        }

        private void MoveSelectedRows(int direction)
        {
            if (SelectedRow == null)
                return;
            var idx = Rows.IndexOf(SelectedRow);
            if (idx < 0)
                return;
            // keyboard moves keep the row's current membership at ambiguous boundaries
            DropAt(SelectedRow, direction < 0 ? idx - 1 : idx + 2, SelectedRow.InConditionBlock);
        }

        private Task CopySelected()
        {
            var selected = SelectedRows.ToList();
            if (selected.Count > 0)
                clipboardService.SetText(DbScriptClipboard.Serialize(selected.Select(r => r.Row)));
            return Task.CompletedTask;
        }

        private async Task PasteRows()
        {
            if (script == null)
                return;
            var text = await clipboardService.GetText();
            if (!DbScriptClipboard.TryDeserialize(text, out var dtos))
                return;

            var insertAt = InsertionIndex();
            var pasted = new List<DbScriptRow>();
            using (script.BulkEdit("Paste rows"))
            {
                foreach (var dto in dtos)
                {
                    DbScriptRow? row = dto.Type switch
                    {
                        DbScriptClipboardRow.ActionType when dto.Line != null =>
                            script.MakeStep(RehomeLine(dto.Line)),
                        DbScriptClipboardRow.WaitType when dto.Duration > 0 =>
                            script.MakeWait(dto.Duration),
                        DbScriptClipboardRow.CommentType when !string.IsNullOrWhiteSpace(dto.Text) =>
                            script.MakeComment(dto.Text!.Trim()),
                        DbScriptClipboardRow.IfType =>
                            script.MakeIf(dto.ConditionId),
                        _ => null,
                    };
                    if (row == null)
                        continue;
                    if (row is not DbScriptIfRow)
                        row.InIf = dto.InIf; // block membership travels with the copied rows
                    script.Rows.Insert(insertAt++, row);
                    pasted.Add(row);
                }
                script.NormalizeIfMembership();
            }

            // pasted if rows may reference conditions this document hasn't loaded yet
            var pastedConditionIds = pasted.OfType<DbScriptIfRow>()
                .Select(r => (uint)r.ConditionId.Value).Where(id => id != 0).ToList();
            if (pastedConditionIds.Count > 0)
            {
                await conditionsStore.LoadClosuresAsync(pastedConditionIds, databaseProvider.GetConditions);
                conditionReadableCache.Clear();
                RefreshConditionReadables();
            }

            // Select what was pasted so it can be moved/deleted right away.
            if (pasted.Count > 0)
            {
                DeselectAllRows();
                suppressSelectionSync = true;
                foreach (var vm in Rows.Where(r => pasted.Contains(r.Row)))
                    vm.IsSelected = true;
                suppressSelectionSync = false;
                SelectedRow = Rows.LastOrDefault(r => pasted.Contains(r.Row));
            }
        }

        private AbstractDbScriptLine RehomeLine(AbstractDbScriptLine line)
        {
            line.Id = script!.ScriptId;
            return line;
        }

        // Drag-drop / keyboard reorder: move the dragged row (or the whole selection when the
        // dragged row is part of it) to the given gap position (0..Rows.Count). All row kinds
        // move the same way — if rows included. preferInside resolves the ambiguous gap at a
        // block boundary (right after the last member, or after an empty if): true = the moved
        // rows join the block, false = they land outside it. Unambiguous gaps ignore it.
        public void DropAt(DbScriptRowViewModel dragged, int insertionIndex, bool preferInside = false)
        {
            if (script == null)
                return;

            var moving = dragged.IsSelected ? SelectedRows.ToList() : new List<DbScriptRowViewModel> { dragged };
            if (moving.Count == 0)
                moving.Add(dragged);
            var movingModels = moving.Select(m => m.Row).ToList();
            var movingSet = new HashSet<DbScriptRow>(movingModels);

            insertionIndex = Math.Clamp(insertionIndex, 0, script.Rows.Count);
            // The gap shifts left by the number of moved rows that were above it.
            var gap = insertionIndex - script.Rows.Take(insertionIndex).Count(movingSet.Contains);

            using (script.BulkEdit("Move rows"))
            {
                // classify the landing gap against the rows that stay put
                var others = script.Rows.Where(r => !movingSet.Contains(r)).ToList();
                var inside = ResolveInside(others, Math.Clamp(gap, 0, others.Count), preferInside);

                foreach (var row in movingModels)
                    script.Rows.Remove(row);
                gap = Math.Clamp(gap, 0, script.Rows.Count);
                foreach (var row in movingModels)
                    script.Rows.Insert(gap++, row);

                // membership travels by position: rows up to the first moved if row take the
                // landing membership; rows after a moved if row belong to IT and keep theirs
                foreach (var row in movingModels)
                {
                    if (row is DbScriptIfRow)
                        break;
                    row.InIf = inside;
                }
                script.NormalizeIfMembership();
            }

            // Re-select the moved rows (their VMs were recreated by the mirror).
            DeselectAllRows();
            suppressSelectionSync = true;
            foreach (var vm in Rows.Where(r => movingSet.Contains(r.Row)))
                vm.IsSelected = true;
            suppressSelectionSync = false;
            SelectedRow = Rows.FirstOrDefault(r => ReferenceEquals(r.Row, movingModels[0]));
        }

        // Does the gap (over the non-moved rows) land inside an if block?
        //  - right under an "if" line: inside when the if has members below (else the X-position
        //    preference decides — an empty if can gain its first member or be passed by),
        //  - strictly between two members: inside,
        //  - right after the last member: ambiguous → the X-position preference decides,
        //  - anywhere else: outside.
        private static bool ResolveInside(List<DbScriptRow> others, int gap, bool preferInside)
        {
            if (gap == 0)
                return false;
            var member = new bool[others.Count];
            var blockOpen = false;
            for (var i = 0; i < others.Count; i++)
            {
                if (others[i] is DbScriptIfRow)
                {
                    blockOpen = true;
                    continue;
                }
                if (blockOpen && !others[i].InIf)
                    blockOpen = false;
                member[i] = blockOpen;
            }
            var belowIsMember = gap < others.Count && member[gap];
            if (others[gap - 1] is DbScriptIfRow)
                return belowIsMember || preferInside;
            if (member[gap - 1])
                return belowIsMember || preferInside;
            return false;
        }

        // Indentation mirrors true block membership (an unbroken InIf run after an if row).
        private void RefreshIndentation()
        {
            if (script == null)
                return;
            var membership = script.ComputeMembership();
            for (var i = 0; i < Rows.Count && i < membership.Count; i++)
                Rows[i].InConditionBlock = membership[i];
            RemoveSelectedCondition.RaiseCanExecuteChanged();
        }

        // ---- inspections ----

        // Cached diagnostics so the async referential pass can be merged with the sync pass without
        // either clobbering the other.
        private IReadOnlyList<IReadOnlyList<DbScriptDiagnostic>> syncDiagnostics = Array.Empty<IReadOnlyList<DbScriptDiagnostic>>();
        private readonly Dictionary<EditableDbScriptStep, List<DbScriptDiagnostic>> asyncDiagnostics = new();
        private int asyncInspectionGeneration;

        private void RunInspections()
        {
            if (script == null)
                return;
            syncDiagnostics = DbScriptInspections.Inspect(script.Steps, script.TypeInfo);
            PublishDiagnostics();
            RunAsyncInspections().ListenErrors();
        }

        private void PublishDiagnostics()
        {
            var stepVms = Rows.OfType<DbScriptStepViewModel>().ToList();
            for (var i = 0; i < stepVms.Count && i < syncDiagnostics.Count; i++)
            {
                var combined = new List<DbScriptDiagnostic>(syncDiagnostics[i]);
                if (asyncDiagnostics.TryGetValue(stepVms[i].Step, out var extra))
                    combined.AddRange(extra);
                stepVms[i].SetDiagnostics(combined);
            }
        }

        // Referential checks that need the database: relay ids and random-template ids that don't
        // exist. Best-effort — skipped silently when disconnected.
        private async Task RunAsyncInspections()
        {
            if (script == null || !mySqlExecutor.IsConnected)
                return;

            var generation = ++asyncInspectionGeneration;

            var relayRefs = CollectReferences(p => p.IsRelayLink);
            var stringTemplateRefs = CollectReferences(p => p.IsStringRandomTemplateLink);
            var relayTemplateRefs = CollectReferences(p => p.IsRelayRandomTemplateLink);

            var missingRelay = await FindMissing("dbscripts_on_relay", relayRefs);
            // template ids are namespaced per type, so a wrong-type id counts as missing
            var missingStringTemplate = await FindMissing("dbscript_random_templates", stringTemplateRefs, "`type` = 0");
            var missingRelayTemplate = await FindMissing("dbscript_random_templates", relayTemplateRefs, "`type` = 1");

            if (generation != asyncInspectionGeneration)
                return; // a newer pass superseded us

            asyncDiagnostics.Clear();
            AddMissingDiagnostics(relayRefs, missingRelay, "relay script");
            AddMissingDiagnostics(stringTemplateRefs, missingStringTemplate, "random text template (type 0)");
            AddMissingDiagnostics(relayTemplateRefs, missingRelayTemplate, "random relay template (type 1)");
            PublishDiagnostics();
        }

        private Dictionary<long, List<EditableDbScriptStep>> CollectReferences(Func<DbScriptEditableParameter, bool> predicate)
        {
            var refs = new Dictionary<long, List<EditableDbScriptStep>>();
            if (script == null)
                return refs;
            foreach (var step in script.Steps)
            {
                foreach (var p in step.UsedParameters)
                {
                    if (!predicate(p) || p.LongHolder is not { } h || h.Value == 0)
                        continue;
                    if (!refs.TryGetValue(h.Value, out var list))
                        refs[h.Value] = list = new List<EditableDbScriptStep>();
                    list.Add(step);
                }
            }
            return refs;
        }

        private async Task<HashSet<long>> FindMissing(string tableName, Dictionary<long, List<EditableDbScriptStep>> refs, string? extraWhere = null)
        {
            var missing = new HashSet<long>();
            var sql = DbScriptExistenceQuery.Build(tableName, "id", refs.Keys, extraWhere);
            if (sql == null)
                return missing;
            try
            {
                var result = await mySqlExecutor.ExecuteSelectSql(sql);
                var idColumn = result.ColumnIndex("id");
                var existing = new HashSet<long>();
                for (var row = 0; row < result.Rows; row++)
                    existing.Add(result.Value<long>(row, idColumn));
                foreach (var id in refs.Keys)
                    if (!existing.Contains(id))
                        missing.Add(id);
            }
            catch
            {
                // table absent on this core / query failure → skip this check
            }
            return missing;
        }

        private void AddMissingDiagnostics(Dictionary<long, List<EditableDbScriptStep>> refs, HashSet<long> missing, string what)
        {
            foreach (var id in missing)
            {
                foreach (var step in refs[id])
                {
                    if (!asyncDiagnostics.TryGetValue(step, out var list))
                        asyncDiagnostics[step] = list = new List<DbScriptDiagnostic>();
                    list.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        $"Referenced {what} {id} does not exist in the database."));
                }
            }
        }

        // ---- action editing (command / parameters / actors) ----

        private async Task ChangeStepCommand(EditableDbScriptStep step)
        {
            var picked = await PickCommand(step.CommandId);
            if (picked == null)
                return;
            var (commandId, variant) = picked.Value;
            using (step.BulkEdit("Change command"))
            {
                step.CommandId = commandId;
                if (variant != null)
                    step.ApplyVariantPresets(variant);
            }
        }

        // SmartScript-style edit dialog (EventAI's ParametersEditViewModel pattern): edits a
        // detached copy live — every parameter is a real editor row (completion combo box for
        // enumerated values, flags combo, checkbox, value+picker), command / source / target are
        // button rows — and applies the copy back on Accept (Cancel discards). No comment field:
        // comments are standalone rows, not action properties.
        private async Task EditActionCommand(EditableDbScriptStep step)
        {
            var copy = new EditableDbScriptStep(step.ToLine(), dataManager, step.TypeInfo, parameterFactory);
            await ShowStepDialog(copy, () => step.CopyFrom(copy.ToLine()));
        }

        // Shows the parameters/command/source/target dialog over a detached step (a copy of an
        // existing step, or a freshly built draft); saveAction runs only on Accept.
        private async Task ShowStepDialog(EditableDbScriptStep copy, Action saveAction)
        {
            // condition picks (TERMINATE_COND) must see the document's unsaved condition edits
            copy.ConditionClosureProvider = root =>
            {
                var closure = conditionsStore.Closure(root);
                return closure.Count > 0 ? closure : null;
            };

            var actionRows = new List<EditableActionData>
            {
                new("Type", "Command", () => ChangeStepCommand(copy).ListenErrors(),
                    copy.ToObservable(s => s.CommandName)),
                new("Source", "Command", () => EditActorSlotCommand(new DbScriptActorSlot(copy, true)).ListenErrors(),
                    copy.ToObservable(s => s.FormattedReadable).Select(_ => copy.ResolveActors().source),
                    copy.ToObservable(s => s.FormattedReadable).Select(_ => !(copy.Command?.UsesSource ?? true))),
                new("Target", "Command", () => EditActorSlotCommand(new DbScriptActorSlot(copy, false)).ListenErrors(),
                    copy.ToObservable(s => s.FormattedReadable).Select(_ => copy.ResolveActors().target),
                    copy.ToObservable(s => s.FormattedReadable).Select(_ => !(copy.Command?.UsesTarget ?? true))),
            };

            var longParams = new List<(ParameterValueHolder<long>, string)>();
            foreach (var holder in copy.DataLongHolders)
                longParams.Add((holder, "Parameters"));
            longParams.Add((copy.AdditionalFlagHolder, "Parameters"));
            // Buddy locator values edit as plain parameter rows (the step relabels/remaps them per
            // the decoded buddy mode and hides them when no buddy is in play).
            longParams.Add((copy.BuddyEntry, "Source / target"));
            longParams.Add((copy.SearchRadius, "Source / target"));
            // No condition_id row: conditions are first-class "if" rows in the editor, never a
            // raw id on the action.

            var floatParams = copy.DataFloatHolders.Select(h => (h, "Parameters")).ToList();

            using var dialog = new DbScriptParametersEditViewModel(
                parameterPickerService,
                copy,
                focusFirst: true,
                longParams,
                floatParams,
                actionParameters: actionRows,
                saveAction: () =>
                {
                    saveAction();
                    ApplyPendingConditionEdits(copy);
                });

            await windowManager.ShowDialog(dialog);
        }

        // Condition trees edited through a parameter's "..." picker (TERMINATE_COND) are stashed
        // on the dialog's detached step copy; on Accept they move into the document's conditions
        // store so the SQL is bundled with the script save. Cancel discards them with the copy.
        private void ApplyPendingConditionEdits(EditableDbScriptStep copy)
        {
            if (copy.PendingConditionEdits == null)
                return;
            foreach (var edit in copy.PendingConditionEdits)
            {
                conditionsStore.ApplyEdit(edit.PreviousEntries, edit.Lines);
                MarkConditionEntriesUsed(edit.FirstFree, edit.Lines);
            }
            copy.PendingConditionEdits = null;
            conditionReadableCache.Clear();
            RefreshConditionReadables();
        }

        // Opens the creature/GO entity picker for a buddy entry (used by the structural editor).
        public async Task<(long value, bool ok)> PickBuddyEntry(bool isGameObject, long current)
        {
            var parameter = parameterFactory.Factory(isGameObject ? "GameobjectParameter" : "CreatureParameter");
            return await parameterPickerService.PickParameter(parameter, current);
        }

        private async Task EditParameterCommand(DbScriptEditableParameter? p)
        {
            if (p == null)
                return;

            // Navigable parameters open a document/table instead of a value picker.
            if (p.IsNavigable && p.LongHolder is { } navHolder)
            {
                var id = (uint)navHolder.Value;
                if (p.IsRelayLink)
                {
                    if (id != 0)
                        eventAggregator.GetEvent<EventRequestOpenItem>()
                            .Publish(new DbScriptSolutionItem(DbScriptType.Relay, id));
                    return;
                }
                if (p.IsRandomTemplateLink && id != 0)
                {
                    // filtered to the referenced template; new rows default to its id
                    await tableEditorPickerService.ShowTable(
                        DatabaseTable.WorldTable("dbscript_random_templates"), $"`id` = {id}", new DatabaseKey(id));
                    return;
                }
                // an unset random template falls through to the regular "..." pick below, which
                // browses/creates templates of the right type and assigns the picked id
            }

            if (p.LongHolder is { } lh)
            {
                var (value, ok) = await parameterPickerService.PickParameter(lh.Parameter, lh.Value);
                if (ok)
                    lh.Value = value;
            }
            else if (p.FloatHolder is { } fh)
            {
                var (value, ok) = await parameterPickerService.PickParameter(fh.Parameter, fh.Value);
                if (ok)
                    fh.Value = value;
            }
        }

        private async Task EditContextLinkCommand(object? ctx)
        {
            switch (ctx)
            {
                case DbScriptEditableParameter p:
                    await EditParameterCommand(p);
                    break;
                case DbScriptActorSlot slot:
                    await EditActorSlotCommand(slot);
                    break;
                case DbScriptConditionSlot conditionSlot:
                    await EditConditionCommand(conditionSlot.IfRow);
                    break;
            }
        }

        // ---- conditions ----

        private string? GetConditionReadable(DbScriptIfRow ifRow, long conditionId)
        {
            if (conditionId <= 0)
                return null;
            var sourceTarget = ComputeConditionSourceTarget(ifRow);
            var key = (conditionId, sourceTarget.Target, sourceTarget.Source);
            if (conditionReadableCache.TryGetValue(key, out var cached))
                return cached;
            var readable = conditionService.BuildReadable((uint)conditionId, conditionsStore.Closure((uint)conditionId), sourceTarget);
            conditionReadableCache[key] = readable;
            return readable;
        }

        // What the core will pass to the condition: the step's RESOLVED source/target pair, i.e.
        // after buddy resolution and the data_flags direction (ExecuteDbscriptCommand receives the
        // final pair and checks condition_id against it). An if block can guard several steps with
        // different flags — disagreeing actors are joined with " / ".
        private MangosConditionSourceTarget ComputeConditionSourceTarget(DbScriptIfRow ifRow)
        {
            var steps = MemberSteps(ifRow);
            if (steps.Count == 0 || script == null)
                return new MangosConditionSourceTarget(
                    script?.TypeInfo.TargetLabel ?? "target",
                    script?.TypeInfo.SourceLabel);
            return ComputeSourceTargetOf(steps);
        }

        private static MangosConditionSourceTarget ComputeSourceTargetOf(IReadOnlyList<EditableDbScriptStep> steps)
        {
            var pairs = steps.Select(s => s.ResolveActors()).Distinct().ToList();
            var target = string.Join(" / ", pairs.Select(p => p.target).Distinct());
            var source = string.Join(" / ", pairs.Select(p => p.source).Distinct());
            return new MangosConditionSourceTarget(target, source);
        }

        // The action rows in the unbroken member run directly below the if row.
        private List<EditableDbScriptStep> MemberSteps(DbScriptIfRow ifRow)
        {
            var result = new List<EditableDbScriptStep>();
            if (script == null)
                return result;
            var membership = script.ComputeMembership();
            var idx = script.Rows.IndexOf(ifRow);
            if (idx < 0)
                return result;
            for (var i = idx + 1; i < script.Rows.Count && membership[i]; i++)
                if (script.Rows[i] is EditableDbScriptStep step)
                    result.Add(step);
            return result;
        }

        private void RefreshConditionReadables()
        {
            foreach (var row in Rows)
                if (row is DbScriptIfViewModel ifVm)
                    ifVm.RefreshText();
        }

        // The if row the selection refers to: the selected if row itself, or the block the
        // selected row is a member of.
        private DbScriptIfRow? ResolveSelectedIfRow()
        {
            if (script == null || SelectedRow == null)
                return null;
            if (SelectedRow is DbScriptIfViewModel ifVm)
                return ifVm.If;
            return script.EnclosingIf(SelectedRow.Row);
        }

        private async Task EditSelectedConditionCommand()
        {
            var ifRow = ResolveSelectedIfRow();
            if (ifRow != null)
                await EditConditionCommand(ifRow);
            else
                await AddConditionForSelection();
        }

        private async Task<uint> GetFirstFreeConditionEntry()
        {
            try
            {
                return (uint)await idGenerator.GetNext(new MangosConditionEntryIdType { LocalMax = conditionsStore.MaxEntry });
            }
            catch (Exception e)
            {
                WDE.Common.LOG.LogError(e.ToString());
                return conditionsStore.MaxEntry + 1;
            }
        }

        private void MarkConditionEntriesUsed(uint firstFree, IReadOnlyList<IMangosConditionLine> lines)
        {
            // the condition editor numbers new nodes sequentially above firstFree on its own -
            // tell the generator, so the next allocation starts above them
            var maxAssigned = lines.Count == 0 ? 0 : lines.Max(l => l.ConditionEntry);
            if (maxAssigned >= firstFree)
                idGenerator.MarkUsed(new MangosConditionEntryIdType(), firstFree, maxAssigned);
        }

        private async Task EditConditionCommand(DbScriptIfRow ifRow)
        {
            var rootId = (uint)ifRow.ConditionId.Value;
            // the id may have been typed manually — make sure its closure is in the store
            await conditionsStore.LoadClosuresAsync(new[] { rootId }, databaseProvider.GetConditions);
            var known = conditionsStore.Closure(rootId);
            var firstFree = await GetFirstFreeConditionEntry();

            var result = await conditionService.EditConditionTree(rootId, known, firstFree, "Condition",
                ComputeConditionSourceTarget(ifRow));
            if (result == null)
                return;

            conditionsStore.ApplyEdit(known.Select(l => l.ConditionEntry), result.Lines);
            MarkConditionEntriesUsed(firstFree, result.Lines);
            conditionReadableCache.Clear();
            // The root id may have changed (ordering repair). Every if row sharing the old root
            // references the same (now rewritten) condition rows — retarget them all (undoable).
            using (script!.BulkEdit("Edit condition"))
            {
                if (rootId != 0)
                {
                    foreach (var row in script.Rows.OfType<DbScriptIfRow>())
                        if (row.ConditionId.Value == rootId && row.ConditionId.Value != result.RootEntry)
                            row.ConditionId.Value = result.RootEntry;
                }
                else if (ifRow.ConditionId.Value != result.RootEntry)
                    ifRow.ConditionId.Value = result.RootEntry;
            }
            RefreshConditionReadables();
        }

        // Wraps the selected contiguous row range in a brand-new if block: opens the condition
        // editor first; on OK inserts the if row above the range and flags the rows as members.
        private async Task AddConditionForSelection()
        {
            if (script == null || SelectedRow == null)
                return;

            var selectedIndices = new List<int>();
            for (var i = 0; i < Rows.Count; i++)
                if (Rows[i].IsSelected)
                    selectedIndices.Add(i);
            var first = selectedIndices.Count > 0 ? selectedIndices[0] : Math.Max(0, Rows.IndexOf(SelectedRow));
            var last = selectedIndices.Count > 0 ? selectedIndices[^1] : first;
            var targets = new List<DbScriptRow>();
            for (var i = first; i <= last && i < Rows.Count; i++)
                targets.Add(Rows[i].Row);

            var guardedSteps = targets.OfType<EditableDbScriptStep>().ToList();
            var sourceTarget = guardedSteps.Count > 0
                ? ComputeSourceTargetOf(guardedSteps)
                : new MangosConditionSourceTarget(script.TypeInfo.TargetLabel, script.TypeInfo.SourceLabel);

            var firstFree = await GetFirstFreeConditionEntry();
            var result = await conditionService.EditConditionTree(0, Array.Empty<IMangosConditionLine>(), firstFree, "New condition",
                sourceTarget);
            if (result == null)
                return;

            conditionsStore.ApplyEdit(Array.Empty<uint>(), result.Lines);
            MarkConditionEntriesUsed(firstFree, result.Lines);
            conditionReadableCache.Clear();

            using (script.BulkEdit("Add if"))
            {
                foreach (var t in targets)
                    if (t is not DbScriptIfRow)
                        t.InIf = true;
                script.Rows.Insert(first, script.MakeIf(result.RootEntry));
                script.NormalizeIfMembership();
            }
            RefreshConditionReadables();
        }

        // High-level source/target picking. The user chooses a semantic actor (original source /
        // target, or a nearby creature / GO / pet / by-guid / pool / string-id buddy); the compiler
        // + codec turn it into the data_flags direction combo and buddy locator. Raw flags are never
        // shown.
        private const long ActorOriginalSource = 0;
        private const long ActorOriginalTarget = 1;
        private const long ActorNearbyCreature = 2;
        private const long ActorNearbyGameobject = 3;
        private const long ActorCreatureByGuid = 4;
        private const long ActorPet = 5;
        private const long ActorByPool = 6;
        private const long ActorByStringId = 7;
        // Target-picker-only: reuse the buddy the source slot already located (a step has one
        // buddy locator, so a buddy source and a different buddy target cannot coexist).
        private const long ActorSameBuddy = 8;

        private static bool IsBuddyActor(long key) => key is >= ActorNearbyCreature and <= ActorByStringId;

        // The static kind(s) an actor choice can resolve to.
        private static DbScriptActorKind KindsOfActor(long key, DbScriptTypeInfo info) => key switch
        {
            ActorOriginalSource => info.SourceKinds,
            ActorOriginalTarget => info.TargetKinds,
            ActorNearbyCreature or ActorCreatureByGuid or ActorPet => DbScriptActorKind.Creature,
            ActorNearbyGameobject => DbScriptActorKind.GameObject,
            ActorByPool or ActorByStringId => DbScriptActorKind.Creature | DbScriptActorKind.GameObject,
            _ => DbScriptActorKind.WorldObject,
        };

        private static bool BuddyActorAllowed(DbScriptCommandDefinition def, long key) => key switch
        {
            ActorNearbyCreature or ActorCreatureByGuid or ActorPet => def.Buddy.AllowsCreature(),
            ActorNearbyGameobject => def.Buddy.AllowsGameObject(),
            _ => true, // pool / string id locate either kind
        };

        // Can the picked source actor execute this command?
        private static bool SourceCompatibleWithCommand(long sourceKey, DbScriptCommandDefinition def, DbScriptTypeInfo info)
        {
            if (!def.UsesSource)
                return sourceKey == ActorOriginalSource; // command ignores its source — only the neutral default keeps it listed
            if (IsBuddyActor(sourceKey) && !BuddyActorAllowed(def, sourceKey))
                return false;
            return DbScriptActorKinds.Compatible(KindsOfActor(sourceKey, info), def.SourceKindMask);
        }

        // Is there ANY pickable target for this command in this script type (given the source)?
        private static bool CommandTargetSatisfiable(DbScriptCommandDefinition def, long sourceKey, DbScriptTypeInfo info)
        {
            if (!def.UsesTarget)
                return true;
            if (DbScriptActorKinds.Compatible(info.SourceKinds, def.TargetKindMask) ||
                DbScriptActorKinds.Compatible(info.TargetKinds, def.TargetKindMask))
                return true;
            // a buddy target: the source's own buddy reused, or a fresh locator
            var buddyKinds = IsBuddyActor(sourceKey)
                ? KindsOfActor(sourceKey, info)
                : DbScriptActorKind.Creature | DbScriptActorKind.GameObject;
            return DbScriptActorKinds.Compatible(buddyKinds, def.TargetKindMask);
        }

        // Builds the actor tiles for one slot. When def is known the list is narrowed to actors
        // its source/target types accept (e.g. a GameObject-target command never offers players).
        private List<DbScriptCommandItem> BuildActorItems(DbScriptCommandDefinition? def, DbScriptTypeInfo info,
            bool isSource, bool includeStructural, long? buddySourceKey = null)
        {
            var required = def == null
                ? DbScriptActorKind.WorldObject
                : (isSource ? def.SourceKindMask : def.TargetKindMask);
            bool Fits(long key) => DbScriptActorKinds.Compatible(KindsOfActor(key, info), required);

            var items = new List<DbScriptCommandItem>();
            var order = 0;
            void Add(long key, string name, string group, string help, string searchTags) =>
                items.Add(new DbScriptCommandItem(group, false, null)
                {
                    Key = key,
                    Name = name,
                    SearchName = $"{name} {searchTags}",
                    Help = help,
                    Order = order++,
                    IsWait = key == DbScriptSelectViewModel.WaitKey,
                });

            if (includeStructural)
            {
                Add(DbScriptSelectViewModel.WaitKey, "Wait", "Timing",
                    "A time gap: every following row executes this much later (squashed into the delay column on save).", "wait delay time gap pause");
                Add(DbScriptSelectViewModel.CommentKey, "Comment", "Timing",
                    "A standalone comment row (saved into the comments column of the next action).", "comment note text");
            }

            const string actorsGroup = "Script actors";
            const string buddyGroup = "A found object (buddy)";
            if (Fits(ActorOriginalSource))
                Add(ActorOriginalSource, info.SourceLabel, actorsGroup, "The script's original source.", "original source self");
            if (Fits(ActorOriginalTarget))
                Add(ActorOriginalTarget, info.TargetLabel, actorsGroup, "The script's original target.", "original target");

            if (buddySourceKey.HasValue && IsBuddyActor(buddySourceKey.Value))
            {
                if (Fits(buddySourceKey.Value))
                    Add(ActorSameBuddy, "The found object (buddy)", buddyGroup,
                        "The same buddy object the source locates.", "buddy same found");
            }
            else
            {
                var allowCreature = def == null || def.Buddy.AllowsCreature();
                var allowGo = def == null || def.Buddy.AllowsGameObject();
                if (allowCreature && Fits(ActorNearbyCreature))
                {
                    Add(ActorNearbyCreature, "A nearby creature (by entry)", buddyGroup,
                        "The nearest creature of a given entry within a search radius.", "buddy nearest npc");
                    Add(ActorCreatureByGuid, "A creature (by GUID)", buddyGroup,
                        "A specific spawned creature, located by its spawn GUID.", "buddy guid npc");
                    Add(ActorPet, "A pet", buddyGroup,
                        "The pet of the original actor (optionally of a given entry).", "buddy pet");
                }
                if (allowGo && Fits(ActorNearbyGameobject))
                    Add(ActorNearbyGameobject, "A nearby gameobject (by entry)", buddyGroup,
                        "The nearest gameobject of a given entry within a search radius.", "buddy nearest go object");
                if (Fits(ActorByPool))
                    Add(ActorByPool, "A pooled spawn (by pool id)", buddyGroup,
                        "The currently spawned object of a spawn pool.", "buddy pool");
                if (Fits(ActorByStringId))
                    Add(ActorByStringId, "An object (by string id)", buddyGroup,
                        "An object tagged with a string id.", "buddy string id tag");
            }

            return items;
        }

        private async Task<long?> PickActorKey(string title, List<DbScriptCommandItem> items, long? preselect)
        {
            using var dialog = new DbScriptSelectViewModel(title, items, preselect)
            {
                DesiredWidth = 640,
                DesiredHeight = 440,
                ItemWidth = 230,
            };
            if (!await windowManager.ShowDialog(dialog) || dialog.SelectedItem == null)
                return null;
            return dialog.SelectedItem.Key;
        }

        // The actor key a slot's current state corresponds to (for preselecting in the picker).
        private static long ActorKeyOf(SourceTargetKind kind, BuddyDescriptor buddy) => kind switch
        {
            SourceTargetKind.OriginalSource => ActorOriginalSource,
            SourceTargetKind.OriginalTarget => ActorOriginalTarget,
            _ => buddy.Mode switch
            {
                BuddyFindMode.ByGuid => ActorCreatureByGuid,
                BuddyFindMode.Pet => ActorPet,
                BuddyFindMode.ByPool => ActorByPool,
                BuddyFindMode.ByStringId => ActorByStringId,
                _ => buddy.IsGameObject ? ActorNearbyGameobject : ActorNearbyCreature,
            },
        };

        private async Task EditActorSlotCommand(DbScriptActorSlot slot)
        {
            var step = slot.Step;
            var current = step.DecodeFlags();

            var items = BuildActorItems(step.Command, step.TypeInfo, slot.IsSource, includeStructural: false);
            var currentKind = slot.IsSource ? current.Direction.Source : current.Direction.Target;
            var title = slot.IsSource ? "Pick source (who acts)" : "Pick target (acted upon)";
            var picked = await PickActorKey(title, items, ActorKeyOf(currentKind, current.Buddy));
            if (!picked.HasValue)
                return;

            var choice = await ResolveActorChoice(picked.Value, current.Buddy);
            if (choice == null)
                return; // cancelled while entering a value

            var updated = DbScriptSourceTargetCompiler.SetSlot(current, slot.IsSource, choice.Value.kind, choice.Value.buddy);
            step.ApplyDecodedFlags(updated);
        }

        // Turns a picked option into (kind, buddy). Gathers the entry/guid/pool/id the option needs;
        // returns null if the user cancels that follow-up pick. Carries over the current buddy's
        // radius/despawned/all-eligible flags when the locator mode is unchanged.
        private async Task<(SourceTargetKind kind, BuddyDescriptor buddy)?> ResolveActorChoice(long option, BuddyDescriptor currentBuddy)
        {
            switch (option)
            {
                case ActorOriginalSource:
                    return (SourceTargetKind.OriginalSource, BuddyDescriptor.None);
                case ActorOriginalTarget:
                    return (SourceTargetKind.OriginalTarget, BuddyDescriptor.None);
                case ActorNearbyCreature:
                {
                    var same = Same(currentBuddy, BuddyFindMode.NearestByEntry, false);
                    var (entry, ok) = await PickBuddyEntry(false, same ? currentBuddy.Entry : 0);
                    if (!ok) return null;
                    // a stale guid/pool value must not leak into the new mode as a bogus radius
                    return (SourceTargetKind.Buddy, WithMode(currentBuddy, BuddyFindMode.NearestByEntry, false, entry, same ? currentBuddy.SearchValue : 0));
                }
                case ActorNearbyGameobject:
                {
                    var same = Same(currentBuddy, BuddyFindMode.NearestByEntry, true);
                    var (entry, ok) = await PickBuddyEntry(true, same ? currentBuddy.Entry : 0);
                    if (!ok) return null;
                    return (SourceTargetKind.Buddy, WithMode(currentBuddy, BuddyFindMode.NearestByEntry, true, entry, same ? currentBuddy.SearchValue : 0));
                }
                case ActorPet:
                {
                    var (entry, ok) = await PickBuddyEntry(false, Same(currentBuddy, BuddyFindMode.Pet, false) ? currentBuddy.Entry : 0);
                    if (!ok) return null;
                    return (SourceTargetKind.Buddy, WithMode(currentBuddy, BuddyFindMode.Pet, false, entry, 0));
                }
                case ActorCreatureByGuid:
                {
                    var (guid, ok) = await PickNumber("Creature GUID", Same(currentBuddy, BuddyFindMode.ByGuid, false) ? currentBuddy.SearchValue : 0);
                    if (!ok) return null;
                    return (SourceTargetKind.Buddy, WithMode(currentBuddy, BuddyFindMode.ByGuid, false, currentBuddy.Entry, guid));
                }
                case ActorByPool:
                {
                    var (pool, ok) = await PickNumber("Pool id", Same(currentBuddy, BuddyFindMode.ByPool, false) ? currentBuddy.SearchValue : 0);
                    if (!ok) return null;
                    return (SourceTargetKind.Buddy, WithMode(currentBuddy, BuddyFindMode.ByPool, false, currentBuddy.Entry, pool));
                }
                case ActorByStringId:
                {
                    var (id, ok) = await PickNumber("String id", Same(currentBuddy, BuddyFindMode.ByStringId, false) ? currentBuddy.Entry : 0);
                    if (!ok) return null;
                    return (SourceTargetKind.Buddy, WithMode(currentBuddy, BuddyFindMode.ByStringId, false, id, 0));
                }
                default:
                    return null;
            }
        }

        private static bool Same(BuddyDescriptor b, BuddyFindMode mode, bool isGo) =>
            b.Mode == mode && b.IsGameObject == isGo;

        private static BuddyDescriptor WithMode(BuddyDescriptor prev, BuddyFindMode mode, bool isGo, long entry, long searchValue) =>
            new(mode, isGo, entry, searchValue, prev.IncludeDespawned, prev.AllEligible);

        // A plain numeric input (guid / pool / string id / wait ms). The generic "Parameter" type
        // gives a number entry picker; the label is advisory only (the picker has its own chrome).
        private Task<(long value, bool ok)> PickNumber(string label, long current) =>
            parameterPickerService.PickParameter("Parameter", current);

        // The dedicated SmartScript-style picker dialog (grouped tiles + search + favourites).
        private async Task<(uint commandId, DbScriptCommandVariant? variant)?> PickCommand(long? current)
        {
            using var dialog = new DbScriptSelectViewModel("Pick command", current, dataManager, favouriteCommands);
            if (!await windowManager.ShowDialog(dialog) || dialog.SelectedItem == null)
                return null;
            return DecodeCommandKey(dialog.SelectedItem.Key);
        }

        private (uint commandId, DbScriptCommandVariant? variant) DecodeCommandKey(long key)
        {
            var commandId = (uint)(key & 0xFFFFFFFF);
            var variantIndex = (int)((key >> 32) - 1);
            var def = dataManager.TryGetCommand(commandId);
            DbScriptCommandVariant? variant = def != null && variantIndex >= 0 && variantIndex < def.Variants.Count
                ? def.Variants[variantIndex]
                : null;
            return (commandId, variant);
        }

        private async Task SaveToDb()
        {
            if (script == null)
                return;
            statusbar.PublishNotification(new PlainNotification(NotificationType.Info, "Saving dbscript to database"));
            try
            {
                var query = await GenerateQuery();
                await mySqlExecutor.ExecuteSql(query);
                history.MarkAsSaved();
                USAGE.Count("document_saved", ("document", "DbScriptEditor"));
                statusbar.PublishNotification(new PlainNotification(NotificationType.Success, "Saved to database"));

                if (remoteConnectorService.IsConnected)
                {
                    try
                    {
                        await remoteConnectorService.ExecuteCommand(
                            new DbScriptReloadRemoteCommand(script.TypeInfo.TableName));
                    }
                    catch (Exception)
                    {
                        // reload is best-effort; a disconnected/failed server must not fail the save
                    }
                }
            }
            catch (Exception e)
            {
                statusbar.PublishNotification(new PlainNotification(NotificationType.Error, "Failed to save"));
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error")
                    .SetMainInstruction("Couldn't save to database")
                    .SetContent(e.Message)
                    .WithOkButton(true).Build());
            }
        }

        // ---- IDocument ----
        public string Title { get; }
        public ImageUri? Icon => new("Icons/document_event_script.png");
        public ICommand Copy { get; }
        public ICommand Cut { get; }
        public ICommand Paste { get; }
        public IAsyncCommand Save => SaveCommand;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;

        public ICommand Undo => UndoCommand;
        public ICommand Redo => RedoCommand;
        public IHistoryManager? History => history;
        public bool IsModified => !history.IsSaved;

        public bool IsLoading
        {
            get => isLoading;
            private set => SetProperty(ref isLoading, value);
        }

        // ---- ISolutionItemDocument ----
        public ISolutionItem SolutionItem => item;
        public bool ShowExportToolbarButtons => true;

        public Task<IQuery> GenerateQuery()
        {
            if (script == null)
                return Task.FromResult(Queries.Empty(DataDatabaseType.World));

            var scriptQuery = exporter.GenerateSql(script);
            if (!conditionsStore.HasChanges)
                return Task.FromResult(scriptQuery);

            // conditions edited inline ship together with the script
            var transaction = Queries.BeginTransaction(DataDatabaseType.World);
            transaction.Comment("conditions used by this script");
            transaction.Add(conditionQueryGenerator.BuildDeleteQuery(conditionsStore.AffectedEntries));
            transaction.Add(conditionQueryGenerator.BuildInsertQuery(conditionsStore.AffectedLines));
            transaction.Add(scriptQuery);
            return Task.FromResult(transaction.Close());
        }
    }
}
