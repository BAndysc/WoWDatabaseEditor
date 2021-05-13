using System;
using System.Collections.Generic;
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
    public class ConditionsDefinitionEditorViewModel: BindableBase, IDocument
    {
        private readonly ConditionsDefinitionEditorHistoryHandler historyHandler;
        private readonly IConditionDataProvider conditionDataProvider;
        private readonly IWindowManager windowManager;
        private readonly IParameterFactory parameterFactory;
        
        public ConditionsDefinitionEditorViewModel(Func<IHistoryManager> historyCreator, IConditionDataProvider conditionDataProvider, IWindowManager windowManager,
            ITaskRunner taskRunner, IParameterFactory parameterFactory)
        {
            this.conditionDataProvider = conditionDataProvider;
            SourceItems = new ObservableCollection<ConditionJsonData>(conditionDataProvider.GetConditions().ToList());
            this.windowManager = windowManager;
            this.parameterFactory = parameterFactory;
            SelectedIndex = -1;

            Save = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Saving conditions definition list", SaveConditions);
            }, () => IsModified);
            Delete = new DelegateCommand(DeleteItem);
            AddItem = new AsyncCommand(AddNewItem);
            EditItem = new AsyncCommand<ConditionJsonData?>(EditCondition);
            // history setup
            historyHandler = new ConditionsDefinitionEditorHistoryHandler(SourceItems);
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

        public ObservableCollection<ConditionJsonData> SourceItems { get; }
        public int SelectedIndex { get; set; }
        
        public DelegateCommand Delete { get; }
        public AsyncCommand AddItem { get; }
        public AsyncCommand<ConditionJsonData?> EditItem { get; }
        private readonly DelegateCommand undoCommand;
        private readonly DelegateCommand redoCommand;

        private void DeleteItem()
        {
            if (SelectedIndex >= 0)
                SourceItems.RemoveAt(SelectedIndex);
        }

        private async Task AddNewItem()
        {
            var obj = new ConditionJsonData();
            obj.Id = SourceItems.Max(x => x.Id) + 1;
            obj.Name = "CONDITION_";
            await OpenEditor(obj, true);
        }

        private async Task EditCondition(ConditionJsonData? item)
        {
            if (item.HasValue)
                await OpenEditor(item.Value, false);
        }

        private async Task OpenEditor(ConditionJsonData item, bool isCreating)
        {
            var vm = new ConditionEditorViewModel(in item, windowManager, parameterFactory);
            if (await windowManager.ShowDialog(vm) && !vm.IsEmpty())
            {
                if (isCreating)
                    SourceItems.Add(vm.Source.ToConditionJsonData());
                else
                {
                    if (SelectedIndex >= 0)
                        SourceItems[SelectedIndex] = vm.Source.ToConditionJsonData();
                }
            }
        }

        private async Task SaveConditions()
        {
            await conditionDataProvider.SaveConditions(SourceItems.ToList());
            History.MarkAsSaved();
        }
        
        public void Dispose()
        {
            historyHandler.Dispose();
        }

        public string Title { get; } = "Conditions Editor";
        public ICommand Undo => undoCommand;
        public ICommand Redo => redoCommand;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
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