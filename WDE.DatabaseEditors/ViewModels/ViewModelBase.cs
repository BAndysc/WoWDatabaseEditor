using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.CustomCommands;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.Utils;
using WDE.DatabaseEditors.ViewModels.SingleRow;
using WDE.MVVM;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.ViewModels
{
    public abstract class ViewModelBase : ObservableBase, ISolutionItemDocument, ISplitSolutionItemQueryGenerator, ITableContext
    {
        protected readonly ISolutionItemNameRegistry solutionItemName;
        protected readonly ISolutionManager solutionManager;
        protected readonly ISolutionTasksService solutionTasksService;
        protected readonly IQueryGenerator queryGenerator;
        protected readonly IDatabaseTableDataProvider databaseTableDataProvider;
        protected readonly IMessageBoxService messageBoxService;
        protected readonly ITaskRunner taskRunner;
        protected readonly IParameterFactory parameterFactory;
        protected readonly ITableDefinitionProvider tableDefinitionProvider;
        protected readonly IItemFromListProvider itemFromListProvider;
        protected readonly ISessionService sessionService;
        protected readonly IDatabaseTableCommandService commandService;
        protected readonly IParameterPickerService parameterPickerService;
        protected readonly IStatusBar statusBar;
        protected readonly IDatabaseQueryExecutor mySqlExecutor;

        protected ViewModelBase(IHistoryManager history,
            DatabaseTableSolutionItem solutionItem,
            ISolutionItemNameRegistry solutionItemName,
            ISolutionManager solutionManager,
            ISolutionTasksService solutionTasksService,
            IEventAggregator eventAggregator,
            IQueryGenerator queryGenerator,
            IDatabaseTableDataProvider databaseTableDataProvider,
            IMessageBoxService messageBoxService,
            ITaskRunner taskRunner,
            IParameterFactory parameterFactory,
            ITableDefinitionProvider tableDefinitionProvider,
            IItemFromListProvider itemFromListProvider,
            ISolutionItemIconRegistry iconRegistry,
            ISessionService sessionService,
            IDatabaseTableCommandService commandService,
            IParameterPickerService parameterPickerService,
            IStatusBar statusBar,
            IDatabaseQueryExecutor mySqlExecutor)
        {
            this.solutionItemName = solutionItemName;
            this.solutionManager = solutionManager;
            this.solutionTasksService = solutionTasksService;
            this.queryGenerator = queryGenerator;
            this.databaseTableDataProvider = databaseTableDataProvider;
            this.messageBoxService = messageBoxService;
            this.taskRunner = taskRunner;
            this.parameterFactory = parameterFactory;
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.sessionService = sessionService;
            this.commandService = commandService;
            this.parameterPickerService = parameterPickerService;
            this.statusBar = statusBar;
            this.mySqlExecutor = mySqlExecutor;
            this.solutionItem = solutionItem;
            Entities = new FlatReadOnlyList<DatabaseEntity>(entities);
            History = history;
            
            undoCommand = new DelegateCommand(History.Undo, CanUndo);
            redoCommand = new DelegateCommand(History.Redo, CanRedo);
            Save = new AsyncAutoCommand(SaveSolutionItem);
            title = solutionItemName.GetName(solutionItem);
            async Task GetTitleAsync()
            {
                Title = await solutionItemName.GetNameAsync(solutionItem);
            }
            GetTitleAsync().ListenErrors();
            Icon = iconRegistry.GetIcon(solutionItem);
            nameGeneratorParameter = parameterFactory.Factory("Parameter");
            
            History.PropertyChanged += (_, _) =>
            {
                undoCommand.RaiseCanExecuteChanged();
                redoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };
            
            tableDefinition = tableDefinitionProvider.GetDefinitionByTableName(solutionItem.TableName)!;
            LoadAndCreateCommands();
            nameGeneratorParameter = parameterFactory.Factory(tableDefinition.Picker);
        }

        public abstract System.IDisposable BulkEdit(string name);

        protected async Task EditParameter(IParameterValue parameterValue, DatabaseEntity entity)
        {
            if (!parameterValue.BaseParameter.HasItems)
                return;
            
            if (parameterValue is IParameterValue<long> valueHolder)
            {
                var result = await parameterPickerService.PickParameter<long>(valueHolder.Parameter, valueHolder.Value, entity);
                if (result.ok)
                    valueHolder.Value = result.value;
            }
            else if (parameterValue is IParameterValue<string> stringValueHolder)
            {
                var result = await parameterPickerService.PickParameter<string>(stringValueHolder.Parameter, stringValueHolder.Value ?? "", entity);
                if (result.ok)
                    stringValueHolder.Value = result.value;             
            }
        }

        public abstract bool ForceRemoveEntity(DatabaseEntity entity);
        public abstract bool ForceInsertEntity(DatabaseEntity entity, int index, bool undoing = false);

        protected abstract IReadOnlyList<DatabaseKey> GenerateKeys();
        protected abstract IReadOnlyList<DatabaseKey>? GenerateDeletedKeys();
        protected abstract Task InternalLoadData(DatabaseTableData data);
        protected abstract void UpdateSolutionItem();
        
        protected Task ScheduleLoading()
        {
            IsLoading = true;
            return taskRunner.ScheduleTask($"Loading {Title}..", InternalLoadData);
        }

        protected virtual Task<IQuery> GenerateSaveQuery() => GenerateQuery();
        
        public virtual Task<IQuery> GenerateQuery()
        {
            return Task.FromResult(queryGenerator
                .GenerateQuery(GenerateKeys(), GenerateDeletedKeys(), new DatabaseTableData(tableDefinition, Entities)));
        }

        protected virtual List<EntityOrigianlField>? GetOriginalFields(DatabaseEntity entity)
        {
            return entity.GetOriginalFields();
        }

        public virtual Task<IList<(ISolutionItem, IQuery)>> GenerateSplitQuery()
        {
            var keys = GenerateKeys();
            IList<(ISolutionItem, IQuery)> split = new List<(ISolutionItem, IQuery)>();
            foreach (var key in keys)
            {
                var entities = Entities.Where(e => e.Key == key).ToList();
                var sql = queryGenerator
                    .GenerateQuery(new List<DatabaseKey>(){key}, null, new DatabaseTableData(tableDefinition, entities));
                var splitItem = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
                splitItem.Entries.Add(new SolutionItemDatabaseEntity(key, entities.Count > 0 && entities[0].ExistInDatabase, entities.Count > 0 && entities[0].ConditionsModified, entities.Count > 0 ? GetOriginalFields(entities[0]) : null));
                split.Add((splitItem, sql));
            }

            return Task.FromResult(split);
        }

        protected async Task SaveSolutionItem()
        {
            if (!await BeforeSaveData())
                return;
            UpdateSolutionItem();
            solutionManager.Refresh(SolutionItem);
            await taskRunner.ScheduleTask($"Export {Title} to database",
                async progress =>
                {
                    progress.Report(0, 2, "Generate query");
                    var query = await GenerateSaveQuery();
                    progress.Report(1, 2, "Execute query");
                    try
                    {
                        await mySqlExecutor.ExecuteSql(tableDefinition, query);
                        History.MarkAsSaved();
                        await AfterSave();
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "Saved to database"));
                    }
                    catch (IMySqlExecutor.QueryFailedDatabaseException e)
                    {
                        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Error")
                            .SetMainInstruction("Couldn't apply SQL")
                            .SetContent(e.Message)
                            .WithOkButton(true)
                            .Build());
                        statusBar.PublishNotification(new PlainNotification(NotificationType.Error, "Couldn't apply SQL: " + e.Message));
                        throw;
                    }
                    progress.ReportFinished();
                });
            Title = await solutionItemName.GetNameAsync(SolutionItem);
        }

        protected virtual Task AfterSave() => Task.CompletedTask;

        protected virtual Task<bool> BeforeLoadData() => Task.FromResult(true);
        protected virtual Task<bool> BeforeSaveData() => Task.FromResult(true);

        protected virtual async Task<DatabaseTableData?> LoadData()
        {
            return await databaseTableDataProvider.Load(solutionItem.TableName, null, null, null, solutionItem.Entries.Select(e => e.Key).ToArray()) as DatabaseTableData;
        }
        
        protected async Task<bool> InternalLoadData()
        {
            if (!await BeforeLoadData())
            {
                IsLoading = false;
                return false;
            }

            try
            {
                var data = await LoadData();

                if (data == null)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                        .SetMainInstruction($"Editor failed to load data from database!")
                        .SetIcon(MessageBoxIcon.Error)
                        .WithOkButton(true)
                        .Build());
                    IsLoading = false;
                    return false;
                }

                solutionItem.UpdateEntitiesWithOriginalValues(data.Entities);

                entities.Do(e => e.RemoveAll());
                entities.RemoveAll();
                await InternalLoadData(data);
                IsLoading = false;
                return true;
            }
            catch (Exception e)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                    .SetMainInstruction($"Editor failed to load data from database!")
                    .SetContent(e.Message)
                    .SetIcon(MessageBoxIcon.Error)
                    .WithOkButton(true)
                    .Build());
                throw;
            }
        }

        private void LoadAndCreateCommands()
        {
            if (tableDefinition.Commands == null)
                return;

            foreach (var commandDefinition in tableDefinition.Commands)
            {
                var cmdExecutor = commandService.FindCommand(commandDefinition.CommandId);
                TableCommandViewModel command;
                if (cmdExecutor != null)
                {
                    command = new TableCommandViewModel(commandDefinition, cmdExecutor, new AsyncAutoCommand(() =>
                    {
                        return messageBoxService.WrapError(() =>
                            WrapBulkEdit(
                                () => WrapBlockingTask(
                                    () => cmdExecutor.Process(commandDefinition,
                                        new DatabaseTableData(tableDefinition, Entities), null, this)
                                ), cmdExecutor.Name));
                    }));
                }
                else
                {
                    var cmdPerKey = commandService.FindPerKeyCommand(commandDefinition.CommandId);
                    if (cmdPerKey == null)  
                        throw new Exception("Command " + commandDefinition.CommandId + " not found!");

                    command = new TableCommandViewModel(commandDefinition, cmdPerKey, new AsyncAutoCommand(() =>
                    {
                        return messageBoxService.WrapError(() =>
                            WrapBulkEdit(
                                () => WrapBlockingTask(() => cmdPerKey.Process(commandDefinition,
                                    new DatabaseTableData(tableDefinition, Entities), GenerateKeys().ToList(), this))
                                , cmdPerKey.Name));
                    }));
                }
                
                if (commandDefinition.KeyBinding != null)
                    KeyBindings.Add(new CommandKeyBinding(command.Command, commandDefinition.KeyBinding, true));
                if (commandDefinition.Usage.HasFlagFast(DatabaseCommandUsage.Toolbar))
                    ToolbarCommands.Add(command);
                if (commandDefinition.Usage.HasFlagFast(DatabaseCommandUsage.ContextMenu))
                    ContextCommands.Add(command);
            }
        }

        protected async Task WrapBulkEdit(Func<Task> t, string name)
        {
            using var disp = BulkEdit(name);
            await t();
        }
        
        protected async Task WrapBlockingTask(Func<Task> t)
        {
            TaskInProgress = true;
            try
            {
                await t();
            }
            finally
            {
                TaskInProgress = false;
            }
        }

        protected string GenerateName(long entity)
        {
            return nameGeneratorParameter.ToString(entity);
        }

        protected async Task<string> GenerateNameAsync(long entity)
        {
            if (nameGeneratorParameter is IAsyncParameter<long> lp)
            {
                return await lp.ToStringAsync(entity, default);
            }
            return nameGeneratorParameter.ToString(entity);
        }
        
        protected DatabaseTableDefinitionJson tableDefinition = null!;
        public DatabaseTableDefinitionJson TableDefinition => tableDefinition;
        // DO NOT REORDER THINGS HERE, OR ELSE UNDO-REDO WILL BE BROKEN :(((
        //public ObservableCollection<DatabaseEntity> Entities { get; } = new();
        public FlatReadOnlyList<DatabaseEntity> Entities { get; }
        protected ObservableCollection<CustomObservableCollection<DatabaseEntity>> entities = new();
        public ObservableCollection<CustomObservableCollection<DatabaseEntity>> EntitiesObservable => entities;

        public ObservableCollection<TableCommandViewModel> ContextCommands { get; } = new();
        public ObservableCollection<TableCommandViewModel> ToolbarCommands { get; } = new();
        public List<CommandKeyBinding> KeyBindings { get; } = new();

        private DelegateCommand undoCommand;
        private DelegateCommand redoCommand;

        private bool CanRedo() => History.CanRedo;
        private bool CanUndo() => History.CanUndo;

        private IParameter<long> nameGeneratorParameter;
        
        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }

        public ImageUri? Icon { get; }
        
        private string title;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        private bool taskInProgress;
        public bool TaskInProgress
        {
            get => taskInProgress;
            private set => SetProperty(ref taskInProgress, value);
        }
        
        public ICommand Undo => undoCommand;
        public ICommand Redo => redoCommand;
        public virtual ICommand Copy => AlwaysDisabledCommand.Command;
        public virtual ICommand Cut => AlwaysDisabledCommand.Command;
        public virtual ICommand Paste => AlwaysDisabledCommand.Command;
        public IAsyncCommand Save { get; }
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified => !History.IsSaved;
        public IHistoryManager History { get; }
        protected DatabaseTableSolutionItem solutionItem;
        public ISolutionItem SolutionItem => solutionItem;
        public virtual DatabaseEntity? FocusedEntity { get; }
        public abstract DatabaseEntity AddRow(DatabaseKey key, int? index = null);
        public abstract IReadOnlyList<DatabaseEntity>? MultiSelectionEntities { get; }
        public abstract bool SupportsMultiSelect { get; }

        public void TryPick(DatabaseEntity entity)
        {
            EntityPicked?.Invoke(entity);
        }
        
        public event Action<DatabaseEntity>? EntityPicked;
    }
}