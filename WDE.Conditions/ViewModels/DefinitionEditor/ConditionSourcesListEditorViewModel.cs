using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Conditions.History;

namespace WDE.Conditions.ViewModels
{
    public class ConditionSourcesListEditorViewModel: BindableBase, IDocument
    {
        private readonly ConditionSourcesEditorHistoryHandler historyHandler;
        private readonly IConditionDataProvider conditionDataProvider;
        private readonly IParameterFactory parameterFactory;
        private readonly IWindowManager windowManager;

        public ConditionSourcesListEditorViewModel(Func<IHistoryManager> historyCreator, IConditionDataProvider conditionDataProvider, IParameterFactory parameterFactory,
            IWindowManager windowManager, ITaskRunner taskRunner)
        {
            this.conditionDataProvider = conditionDataProvider;
            this.parameterFactory = parameterFactory;
            this.windowManager = windowManager;
            
            SourceItems = new ObservableCollection<ConditionSourcesJsonData>(conditionDataProvider.GetConditionSources());
            SelectedIndex = -1;
            Save = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Saving Condition Sources", SaveSources);
            });
            Delete = new DelegateCommand(DeleteSource);
            AddItem = new DelegateCommand(AddSource);
            EditItem = new AsyncCommand<ConditionSourcesJsonData?>(EditSource);
            // history setup
            historyHandler = new ConditionSourcesEditorHistoryHandler(SourceItems);
            History = historyCreator();
            undoCommand = new DelegateCommand(History.Undo, () => History.CanUndo);
            redoCommand = new DelegateCommand(History.Redo, () => History.CanRedo);
            History.PropertyChanged += (sender, args) =>
            {
                undoCommand.RaiseCanExecuteChanged();
                redoCommand.RaiseCanExecuteChanged();
                IsModified = !History.IsSaved;
            };
            History.AddHandler(historyHandler);
        }

        public ObservableCollection<ConditionSourcesJsonData> SourceItems { get; }
        public int SelectedIndex { get; set; }
        
        public DelegateCommand Delete { get; }
        public DelegateCommand AddItem { get; }
        public AsyncCommand<ConditionSourcesJsonData?> EditItem { get; }
        private readonly DelegateCommand undoCommand;
        private readonly DelegateCommand redoCommand;

        private void DeleteSource()
        {
            if (SelectedIndex >= 0)
                SourceItems.RemoveAt(SelectedIndex);
        }

        private void AddSource()
        {
            var obj = new ConditionSourcesJsonData();
            obj.Id = SourceItems.Max(x => x.Id) + 1;
            obj.Name = "CONDITION_SOURCE_TYPE_";
            OpenEditor(obj, true);
        }

        private async Task EditSource(ConditionSourcesJsonData? item)
        {
            if (item.HasValue)
                await OpenEditor(item.Value, false);
        }

        private async Task OpenEditor(ConditionSourcesJsonData item, bool isCreating)
        {
            var vm = new ConditionSourceEditorViewModel(in item, parameterFactory, windowManager);
            if (await windowManager.ShowDialog(vm) && !vm.IsEmpty())
            {
                if (isCreating)
                    SourceItems.Add(vm.Source.ToConditionSourcesJsonData());
                else
                {
                    if (SelectedIndex >= 0)
                        SourceItems[SelectedIndex] = vm.Source.ToConditionSourcesJsonData();
                }
            }
        }
        
        private async Task SaveSources()
        {
            //await conditionDataProvider.SaveConditionSources(SourceItems.ToList());
            History.MarkAsSaved();
        }
        
        public void Dispose()
        {
            historyHandler.Dispose();
        }

        public string Title { get; } = "Condition Sources Editor";
        public ICommand Undo => undoCommand;
        public ICommand Redo => redoCommand;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; }= AlwaysDisabledCommand.Command;
        public ICommand Paste { get; }= AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            set
            {
                isModified = value;
                RaisePropertyChanged(nameof(IsModified));
            }
        }
        public IHistoryManager History { get; }
    }
}