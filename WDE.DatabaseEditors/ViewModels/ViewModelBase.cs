using System.Collections;
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
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Solution;
using WDE.MVVM;

namespace WDE.DatabaseEditors.ViewModels
{
    public abstract class ViewModelBase : ObservableBase, ISolutionItemDocument
    {
        private readonly ISolutionItemNameRegistry solutionItemName;
        private readonly ISolutionManager solutionManager;
        private readonly ISolutionTasksService solutionTasksService;
        private readonly IQueryGenerator queryGenerator;
        private readonly IDatabaseTableDataProvider databaseTableDataProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly ITaskRunner taskRunner;
        private readonly IParameterFactory parameterFactory;

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
            IParameterFactory parameterFactory)
        {
            this.solutionItemName = solutionItemName;
            this.solutionManager = solutionManager;
            this.solutionTasksService = solutionTasksService;
            this.queryGenerator = queryGenerator;
            this.databaseTableDataProvider = databaseTableDataProvider;
            this.messageBoxService = messageBoxService;
            this.taskRunner = taskRunner;
            this.parameterFactory = parameterFactory;
            this.solutionItem = solutionItem;
            History = history;
            
            undoCommand = new DelegateCommand(History.Undo, CanUndo);
            redoCommand = new DelegateCommand(History.Redo, CanRedo);
            Save = new DelegateCommand(SaveSolutionItem);
            title = solutionItemName.GetName(solutionItem);
            nameGeneratorParameter = parameterFactory.Factory("Parameter");
            
            History.PropertyChanged += (_, _) =>
            {
                undoCommand.RaiseCanExecuteChanged();
                redoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };
            
            AutoDispose(eventAggregator.GetEvent<EventRequestGenerateSql>()
                .Subscribe(ExecuteSql));
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

        private void ExecuteSql(EventRequestGenerateSqlArgs args)
        {
            if (args.Item is not DatabaseTableSolutionItem dbEditItem) 
                return;
            
            if (!SolutionItem.Equals(dbEditItem)) 
                return;
            
            args.Sql = queryGenerator.GenerateQuery(GenerateKeys(), new DatabaseTableData(tableDefinition, Entities));
        }

        private void SaveSolutionItem()
        {
            UpdateSolutionItem();
            solutionManager.Refresh(SolutionItem);
            solutionTasksService.SaveSolutionToDatabaseTask(SolutionItem);
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
            
            Entities.Clear();
            tableDefinition = data.TableDefinition;
            nameGeneratorParameter = parameterFactory.Factory(tableDefinition.Picker);
            await InternalLoadData(data);
            IsLoading = false;
        }

        protected string GenerateName(uint entity)
        {
            return nameGeneratorParameter.ToString(entity);
        }
        
        protected DatabaseTableDefinitionJson tableDefinition = null!;
        public ObservableCollection<DatabaseEntity> Entities { get; } = new();

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

        private string title;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
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
    }
}