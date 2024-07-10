using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.Utils;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.ViewModels.OneToOneForeignKey;

public partial class OneToOneForeignKeyViewModel : ObservableBase, IDialog, ISolutionItemDocument, IClosableDialog
{
    private readonly IDatabaseTableDataProvider dataProvider;
    private readonly IDatabaseTableModelGenerator modelGenerator;
    private readonly ISessionService sessionService;
    private readonly IParameterFactory parameterFactory;
    private readonly IClipboardService clipboardService;
    private readonly IWindowManager windowManager;
    private readonly IQueryGenerator queryGenerator;
    private readonly IDatabaseQueryExecutor mySqlExecutor;
    private readonly IMessageBoxService messageBoxService;
    private readonly IParameterPickerService parameterPickerService;
    private readonly ISolutionTasksService solutionTasksService;
    private readonly IMetaColumnsSupportService metaColumnsSupportService;
    private readonly DatabaseTableDefinitionJson tableDefinition;
    private readonly DatabaseKey key;
    private readonly bool noSaveMode;

    private bool wasPresentInDatabase;
    [Notify] private bool presentInDatabase;
    private DatabaseEntityViewModel row;
    
    private HashSet<ColumnFullName> forceUpdateCells = new HashSet<ColumnFullName>();

    public DatabaseEntityViewModel Row
    {
        get => row;
        set
        {
            var old = row;
            if (old != null)
            {
                old.Entity.OnAction -= OnEntityAction;
            }
            SetProperty(ref row, value);
            if (value != null)
                value.Entity.OnAction += OnEntityAction;
        }
    }

    public OneToOneForeignKeyViewModel(
        IDatabaseTableDataProvider dataProvider,
        IDatabaseTableModelGenerator modelGenerator,
        ISessionService sessionService,
        IParameterFactory parameterFactory,
        ITaskRunner taskRunner,
        IClipboardService clipboardService,
        IEventAggregator eventAggregator,
        IWindowManager windowManager,
        IQueryGenerator queryGenerator,
        IDatabaseQueryExecutor mySqlExecutor,
        IMessageBoxService messageBoxService,
        ISolutionItemEditorRegistry editorRegistry,
        IHistoryManager history,
        IParameterPickerService parameterPickerService,
        ISolutionTasksService solutionTasksService,
        IMetaColumnsSupportService metaColumnsSupportService,
        DatabaseTableDefinitionJson tableDefinition,
        DatabaseKey key,
        bool noSaveMode)
    {
        //if (tableDefinition.RecordMode != RecordMode.SingleRow)
        //    throw new Exception("Only single row mode is supported, otherwise it won't be one - to - one relation");
        
        this.dataProvider = dataProvider;
        this.modelGenerator = modelGenerator;
        this.sessionService = sessionService;
        this.parameterFactory = parameterFactory;
        this.clipboardService = clipboardService;
        this.windowManager = windowManager;
        this.queryGenerator = queryGenerator;
        this.mySqlExecutor = mySqlExecutor;
        this.messageBoxService = messageBoxService;
        this.parameterPickerService = parameterPickerService;
        this.solutionTasksService = solutionTasksService;
        this.metaColumnsSupportService = metaColumnsSupportService;
        this.tableDefinition = tableDefinition;
        this.History = history;
        this.key = key;
        this.noSaveMode = noSaveMode;
        solutionItem = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
        Undo = history.UndoCommand();
        Redo = history.RedoCommand();
        Accept = new DelegateCommand(() =>
        {
            AskIfSave(false).ListenErrors();
        });
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        ExecuteChangedCommand = noSaveMode ? new AsyncAutoCommand(() => Task.CompletedTask, () => false) : new AsyncAutoCommand(async () =>
        {
            await SaveData();
            eventAggregator.GetEvent<DatabaseTableChanged>().Publish(tableDefinition.Id);
            if (sessionService.IsOpened && !sessionService.IsPaused)
            {
                UpdateSolutionItemWithEverything();
                await taskRunner.ScheduleTask("Update session", async () => await sessionService.UpdateQuery(this));
            }

            History.MarkAsSaved();
            forceUpdateCells.Clear();
        });
        Save = ExecuteChangedCommand;
        CopyCurrentSqlCommand = new AsyncAutoCommand(async () =>
        {
            await taskRunner.ScheduleTask("Generating SQL",
                async () => { clipboardService.SetText((await GenerateQuery()).QueryString);});
        });
        GenerateCurrentSqlCommand = new AsyncAutoCommand(async () =>
        {
            var sql = await GenerateThisQueryOnly();
            var item = new MetaSolutionSQL(new JustQuerySolutionItem(sql));
            using var editor = editorRegistry.GetEditor(item);
            await windowManager.ShowDialog((IDialog)editor);
        });
        OpenParameterWindow = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel>(EditParameter);
        RevertCommand = new DelegateCommand<SingleRecordDatabaseCellViewModel?>(cell =>
        {
            cell!.ParameterValue!.Revert();
        }, cell => cell != null && cell.CanBeReverted && (cell?.TableField?.IsModified ?? false));
        SetNullCommand = new DelegateCommand<SingleRecordDatabaseCellViewModel?>(SetToNull, vm => vm != null && vm.CanBeSetToNull);
        
        On(() => PresentInDatabase, @is =>
        {
            if (@is)
            {
                handler.PushAction(new AnonymousHistoryAction("Make present in the database", () =>
                {
                    PresentInDatabase = false;
                }, () =>
                {
                    PresentInDatabase = true;
                }));
            }
            else
            {
                handler.PushAction(new AnonymousHistoryAction("Delete from the database", () =>
                {
                    PresentInDatabase = true;
                }, () =>
                {
                    PresentInDatabase = false;
                }));
            }
            if (@is && Row == null)
            {
                Row = CreateEmpty();
            }
        });

        Row = row = CreateEmpty();
        Title = this.tableDefinition.TableName + " of " + key;

        Load().ListenErrors();
    }

    private Task EditParameter(SingleRecordDatabaseCellViewModel cell)
    {
        if (cell.ParameterValue != null)
            return EditParameter(cell.ParameterValue);
        return Task.CompletedTask;
    }
    
    protected async Task EditParameter(IParameterValue parameterValue)
    {
        if (!parameterValue.BaseParameter.HasItems)
            return;
            
        if (parameterValue is IParameterValue<long> valueHolder)
        {
            var result = await parameterPickerService.PickParameter<long>(valueHolder.Parameter, valueHolder.Value, Row.Entity);
            if (result.ok)
                valueHolder.Value = result.value;
        }
        else if (parameterValue is IParameterValue<string> stringValueHolder)
        {
            var result = await parameterPickerService.PickParameter<string>(stringValueHolder.Parameter, stringValueHolder.Value ?? "", Row.Entity);
            if (result.ok)
                stringValueHolder.Value = result.value;             
        }
    }
    
    private void SetToNull(SingleRecordDatabaseCellViewModel? view)
    {
        if (view != null && view.CanBeNull && !view.IsReadOnly) 
            view.ParameterValue?.SetNull();
    }

    private void OnEntityAction(IHistoryAction action)
    {
        handler.PushAction(action);
    }

    private async Task SaveData()
    {
        var query = await GenerateThisQueryOnly();
        try
        {
            await mySqlExecutor.ExecuteSql(tableDefinition, query);

            if (solutionTasksService.CanReloadRemotely)
            {
                var pseudo = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
                pseudo.Entries.Add(new SolutionItemDatabaseEntity(key, false, false));
                await solutionTasksService.ReloadSolutionRemotelyTask(pseudo);
            }
        }
        catch (Exception e)
        {
            await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Error")
                .SetMainInstruction("Error while saving")
                .SetContent(e.Message)
                .WithOkButton(true)
                .Build());
        }
    }

    private DatabaseTableSolutionItem solutionItem;
    public ISolutionItem SolutionItem => solutionItem;
    
    protected void UpdateSolutionItemWithEverything()
    {
        var pseudo = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
        var found = (DatabaseTableSolutionItem?)sessionService.Find(pseudo);
        if (found == null)
            solutionItem = pseudo;
        else
            solutionItem = (DatabaseTableSolutionItem)found.Clone();
        
        var previousData = solutionItem.Entries.Where(e => e.Key != key);

        if (PresentInDatabase)
        {
            solutionItem.Entries = new []{new SolutionItemDatabaseEntity(key, Row!.Entity.ExistInDatabase, Row!.Entity.ConditionsModified, Row.Entity.GetOriginalFields())}
                .Union(previousData)
                .ToList();
        } else if (wasPresentInDatabase && !presentInDatabase)
        {
            solutionItem.Entries = previousData.ToList();
            solutionItem.DeletedEntries.Add(key);
        }
    }

    public async Task<IQuery> GenerateQuery()
    {
        UpdateSolutionItemWithEverything();

        var newData = PresentInDatabase ? new List<DatabaseEntity>() { Row!.Entity } : new List<DatabaseEntity>();

        var allKeys = solutionItem.Entries.Select(x => x.Key).ToList();
        var deleteKeys = solutionItem.DeletedEntries;
        var newDataKeys = PresentInDatabase ? new[] { key } : Array.Empty<DatabaseKey>();
        var oldKeys = allKeys.Except(newDataKeys).ToArray();
            
        IDatabaseTableData? data = null;
        if (oldKeys.Length > 0)
        {
            data = await dataProvider.Load(tableDefinition.Id, null, null, oldKeys.Length, oldKeys);
            if (data == null)
                return Queries.Raw(tableDefinition.DataDatabaseType, "ERROR");
            solutionItem.UpdateEntitiesWithOriginalValues(data.Entities);
        }

        data = new DatabaseTableData(tableDefinition, data == null ? newData : data.Entities.Union(newData).ToList());
            
        var query = queryGenerator.GenerateQuery(allKeys,  deleteKeys, data);

        return query;
    }
    
    private async Task<IQuery> GenerateThisQueryOnly()
    {
        if (wasPresentInDatabase && !PresentInDatabase)
            return queryGenerator.GenerateDeleteQuery(tableDefinition, key);

        if (!wasPresentInDatabase && !PresentInDatabase)
            return Queries.Empty(tableDefinition.DataDatabaseType);

        var query = queryGenerator.GenerateQuery(new[] { key }, null, new DatabaseTableData(tableDefinition, new[] { Row!.Entity }));
        
        IMultiQuery multi = Queries.BeginTransaction(tableDefinition.DataDatabaseType);
        multi.Add(query);
        foreach (var pair in forceUpdateCells)
        {
            var entity = row.Entity;
            var field = entity.GetCell(pair);
            if (field == null)
                continue;
            multi.Add(queryGenerator.GenerateUpdateFieldQuery(tableDefinition, entity, field));
        }
        return multi.Close();
    }
    
    private void OnRowChangedCell(DatabaseEntityViewModel entity, SingleRecordDatabaseCellViewModel cell, ColumnFullName columnName)
    {
        if (!cell.IsModified && entity.Entity.ExistInDatabase)
        {
            Debug.Assert(!entity.Entity.Phantom);
            forceUpdateCells.Add(columnName);
            History.MarkNoSave();
        }
    }
    
    private DatabaseEntityViewModel Create(DatabaseEntity entity)
    {
        var pseudoItem = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
        var savedItem = sessionService.Find(pseudoItem);
        if (savedItem is DatabaseTableSolutionItem savedTableItem)
            savedTableItem.UpdateEntitiesWithOriginalValues(new List<DatabaseEntity>() { entity });

        var row = new DatabaseEntityViewModel(entity);
        row.ChangedCell += OnRowChangedCell;
        AutoDispose(new ActionDisposable(() =>
        {
            row.ChangedCell -= OnRowChangedCell;
        }));
        
        int columnIndex = 0;
        foreach (var group in tableDefinition.Groups)
        {
            var g = new DatabaseFieldsGroup(group.Name);
            row.Groups.Add(g);
            foreach (var column in group.Fields)
            {
                SingleRecordDatabaseCellViewModel cellViewModel;

                if (tableDefinition.PrimaryKey.Contains(column.DbColumnFullName))
                    continue;
                
                if (column.IsConditionColumn)
                {
                    throw new Exception("One to one conditions editing not supported (but it could be, but is there a need?)");
                }
                else if (column.IsMetaColumn)
                {
                    if (column.Meta!.StartsWith("expression:"))
                    {
                        throw new NotImplementedException();
                    }
                    else if (column.Meta!.StartsWith("customfield:"))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var (cmd, name) = metaColumnsSupportService.GenerateCommand(null, tableDefinition.DataDatabaseType, column.Meta!, entity, key);
                        cellViewModel = new SingleRecordDatabaseCellViewModel(columnIndex, column.Name, cmd, row, entity, name);
                    }
                }
                else
                {
                    var cell = entity.GetCell(column.DbColumnFullName);
                    if (cell == null)
                        throw new Exception("this should never happen");

                    IParameterValue parameterValue = null!;
                    if (cell is DatabaseField<long> longParam)
                    {
                        IParameter<long> parameter = parameterFactory.Factory(column.ValueType);
                        parameterValue = new ParameterValue<long, DatabaseEntity>(entity, longParam.Current, longParam.Original, parameter);
                    }
                    else if (cell is DatabaseField<string> stringParam)
                    {
                        if (column.AutogenerateComment != null)
                        {
                            stringParam.Current.Value = stringParam.Current.Value.GetComment(column.CanBeNull);
                            stringParam.Original.Value = stringParam.Original.Value.GetComment(column.CanBeNull);
                        }

                        parameterValue = new ParameterValue<string, DatabaseEntity>(entity, stringParam.Current, stringParam.Original, parameterFactory.FactoryString(column.ValueType));
                    }
                    else if (cell is DatabaseField<float> floatParameter)
                    {
                        parameterValue = new ParameterValue<float, DatabaseEntity>(entity, floatParameter.Current, floatParameter.Original, FloatParameter.Instance);
                    }

                    parameterValue.DefaultIsBlank = column.IsZeroBlank;
                    cellViewModel = new SingleRecordDatabaseCellViewModel(columnIndex, column, row, entity, cell, parameterValue);
                }

                g.Cells.Add(cellViewModel);
                columnIndex++;
            }   
        }

        if (row.Groups.Count == 1)
            row.Groups[0].ShowHeader = false;

        return row;
    }

    private DatabaseEntityViewModel CreateEmpty()
    {
        var empty = modelGenerator.CreateEmptyEntity(tableDefinition, key, false);
        return Create(empty);
    }

    private async Task Load()
    {
        var data = await dataProvider.Load(tableDefinition.Id, null, null, 1, new DatabaseKey[] { key });

        if (data == null)
            return;
        
        row?.DisposeAllCells();
        wasPresentInDatabase = data.Entities.Count > 0;
        if (data.Entities.Count == 0)
        {
            PresentInDatabase = false;
            Row = CreateEmpty();
        }
        else
        {
            Row = Create(data.Entities[0]);
            PresentInDatabase = true;
        }
        History.AddHandler(handler);
    }
    
    public int DesiredWidth => 560;
    public int DesiredHeight => 640;
    public string Title { get; }
    public ICommand Copy => new AlwaysDisabledCommand();
    public ICommand Cut => new AlwaysDisabledCommand();
    public ICommand Paste => new AlwaysDisabledCommand();
    public DelegateCommand<SingleRecordDatabaseCellViewModel?> RevertCommand { get; }
    public DelegateCommand<SingleRecordDatabaseCellViewModel?> SetNullCommand { get; }
    public AsyncAutoCommand<SingleRecordDatabaseCellViewModel> OpenParameterWindow { get; }
    public IAsyncCommand Save { get; }

    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose { get; set; }
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public IAsyncCommand ExecuteChangedCommand { get; }
    public ICommand GenerateCurrentSqlCommand { get; }
    public ICommand CopyCurrentSqlCommand { get; }

    public event Action? CloseCancel;
    public event Action? CloseOk;
    public ICommand Undo { get; }
    public ICommand Redo { get; }
    private HistoryHandler handler = new();
    public IHistoryManager History { get; }
    public bool IsModified => !History.IsSaved;
    public Task? PendingSaveTask { get; private set; } = Task.CompletedTask;
    
    private async Task AskIfSave(bool cancel)
    {
        if (IsModified)
        {
            var result = await messageBoxService.ShowDialog(new MessageBoxFactory<int>()
                .SetTitle("Save changes")
                .SetMainInstruction($"{Title} has unsaved changes")
                .SetContent("Do you want to save them?")
                .WithNoButton(1)
                .WithYesButton(2)
                .WithCancelButton(0)
                .Build());
            if (result == 0)
                return;
            if (result == 2)
                PendingSaveTask = ExecuteChangedCommand.ExecuteAsync();
        }
        
        if (cancel)
            CloseCancel?.Invoke();
        else
            CloseOk?.Invoke();
    }
    
    public void OnClose()
    {
        if (IsModified)
        {
            AskIfSave(true).ListenErrors();
        }
        else
            CloseCancel?.Invoke();
    }

    public event Action? Close;
}