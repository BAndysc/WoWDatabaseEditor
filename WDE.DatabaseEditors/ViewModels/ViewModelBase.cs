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
using WDE.DatabaseEditors.Solution;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels
{
    public abstract class ViewModelBase : ObservableBase, ISolutionItemDocument, ISplitSolutionItemQueryGenerator, IAddRowKey
    {
        private readonly ISolutionItemNameRegistry solutionItemName;
        private readonly ISolutionManager solutionManager;
        private readonly ISolutionTasksService solutionTasksService;
        private readonly IQueryGenerator queryGenerator;
        private readonly IDatabaseTableDataProvider databaseTableDataProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly ITaskRunner taskRunner;
        private readonly IParameterFactory parameterFactory;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ISessionService sessionService;
        private readonly IDatabaseTableCommandService commandService;
        private readonly IParameterPickerService parameterPickerService;

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
            IParameterPickerService parameterPickerService)
        {
            this.solutionItemName = solutionItemName;
            this.solutionManager = solutionManager;
            this.solutionTasksService = solutionTasksService;
            this.queryGenerator = queryGenerator;
            this.databaseTableDataProvider = databaseTableDataProvider;
            this.messageBoxService = messageBoxService;
            this.taskRunner = taskRunner;
            this.parameterFactory = parameterFactory;
            this.itemFromListProvider = itemFromListProvider;
            this.sessionService = sessionService;
            this.commandService = commandService;
            this.parameterPickerService = parameterPickerService;
            this.solutionItem = solutionItem;
            History = history;
            
            undoCommand = new DelegateCommand(History.Undo, CanUndo);
            redoCommand = new DelegateCommand(History.Redo, CanRedo);
            Save = new DelegateCommand(SaveSolutionItem);
            title = solutionItemName.GetName(solutionItem);
            Icon = iconRegistry.GetIcon(solutionItem);
            nameGeneratorParameter = parameterFactory.Factory("Parameter");
            
            History.PropertyChanged += (_, _) =>
            {
                undoCommand.RaiseCanExecuteChanged();
                redoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };
            
            tableDefinition = tableDefinitionProvider.GetDefinition(solutionItem.DefinitionId)!;
            nameGeneratorParameter = parameterFactory.Factory(tableDefinition.Picker);
        }

        protected abstract System.IDisposable BulkEdit(string name);

        protected async Task EditParameter(IParameterValue parameterValue)
        {
            if (!parameterValue.BaseParameter.HasItems)
                return;
            
            if (parameterValue is ParameterValue<long> valueHolder)
            {
                var result = await parameterPickerService.PickParameter<long>(valueHolder.Parameter, valueHolder.Value);
                if (result.ok)
                    valueHolder.Value = result.value;
            }
            else if (parameterValue is ParameterValue<string> stringValueHolder)
            {
                var result = await parameterPickerService.PickParameter<string>(stringValueHolder.Parameter, stringValueHolder.Value ?? "");
                if (result.ok)
                    stringValueHolder.Value = result.value;             
            }
        }

        public abstract bool ForceRemoveEntity(DatabaseEntity entity);
        public abstract bool ForceInsertEntity(DatabaseEntity entity, int index);

        protected abstract ICollection<uint> GenerateKeys();
        protected abstract Task InternalLoadData(DatabaseTableData data);
        protected abstract void UpdateSolutionItem();
        
        protected void ScheduleLoading()
        {
            IsLoading = true;
            taskRunner.ScheduleTask($"Loading {Title}..", LoadTableDefinition);
        }

        public Task<string> GenerateQuery()
        {
            return Task.FromResult(queryGenerator
                .GenerateQuery(GenerateKeys(), new DatabaseTableData(tableDefinition, Entities)).QueryString);
        }

        protected virtual List<EntityOrigianlField>? GetOriginalFields(DatabaseEntity entity) => null;

        public Task<IList<(ISolutionItem, string)>> GenerateSplitQuery()
        {
            var keys = GenerateKeys();
            IList<(ISolutionItem, string)> split = new List<(ISolutionItem, string)>();
            foreach (var key in keys)
            {
                var entities = Entities.Where(e => e.Key == key).ToList();
                var sql = queryGenerator
                    .GenerateQuery(new List<uint>(){key}, new DatabaseTableData(tableDefinition, entities)).QueryString;
                var splitItem = new DatabaseTableSolutionItem(tableDefinition.Id);
                splitItem.Entries.Add(new SolutionItemDatabaseEntity(key, entities.Count > 0 ? entities[0].ExistInDatabase : false, entities.Count > 0 ? GetOriginalFields(entities[0]) : null));
                split.Add((splitItem, sql));
            }

            return Task.FromResult(split);
        }

        private void SaveSolutionItem()
        {
            UpdateSolutionItem();
            solutionManager.Refresh(SolutionItem);
            solutionTasksService.SaveSolutionToDatabaseTask(this);
            History.MarkAsSaved();
            Title = solutionItemName.GetName(SolutionItem);
        }
        
        private async Task LoadTableDefinition()
        {
            var data = await databaseTableDataProvider.Load(solutionItem.DefinitionId, solutionItem.Entries.Select(e => e.Key).ToArray()) as DatabaseTableData;

            if (data == null)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                    .SetMainInstruction($"Editor failed to load data from database!")
                    .SetIcon(MessageBoxIcon.Error)
                    .WithOkButton(true)
                    .Build());
                return;
            }

            solutionItem.UpdateEntitiesWithOriginalValues(data.Entities);

            LoadAndCreateCommands(data);
            
            Entities.Clear();
            await InternalLoadData(data);
            IsLoading = false;
        }

        private void LoadAndCreateCommands(DatabaseTableData data)
        {
            if (data.TableDefinition.Commands == null)
                return;

            foreach (var command in data.TableDefinition.Commands)
            {
                var cmd = commandService.FindCommand(command.CommandId);
                if (cmd != null)
                {
                    Commands.Add(new TableCommandViewModel(cmd, new AsyncAutoCommand( () =>
                    {
                        return messageBoxService.WrapError(() => 
                            WrapBulkEdit(
                            () => WrapBlockingTask(
                                () => cmd.Process(command, new DatabaseTableData(data.TableDefinition, Entities))
                                ), cmd.Name));
                    })));
                }
                else
                {
                    var cmdPerKey = commandService.FindPerKeyCommand(command.CommandId);
                    if (cmdPerKey == null)  
                        throw new Exception("Command " + command.CommandId + " not found!");

                    Commands.Add(new TableCommandViewModel(cmdPerKey, new AsyncAutoCommand( () =>
                    {
                        return messageBoxService.WrapError(() => 
                            WrapBulkEdit(
                                () => WrapBlockingTask(() => cmdPerKey.Process(command,
                            new DatabaseTableData(data.TableDefinition, Entities), GenerateKeys(), this))
                                    , cmdPerKey.Name));
                    })));
                }
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

        protected string GenerateName(uint entity)
        {
            return nameGeneratorParameter.ToString(entity);
        }
        
        protected DatabaseTableDefinitionJson tableDefinition = null!;
        public DatabaseTableDefinitionJson TableDefinition => tableDefinition;
        public ObservableCollection<DatabaseEntity> Entities { get; } = new();

        public ObservableCollection<TableCommandViewModel> Commands { get; } = new();

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
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified => !History.IsSaved;
        public IHistoryManager History { get; }
        protected DatabaseTableSolutionItem solutionItem;
        public ISolutionItem SolutionItem => solutionItem;
        public abstract DatabaseEntity AddRow(uint key);
    }
}