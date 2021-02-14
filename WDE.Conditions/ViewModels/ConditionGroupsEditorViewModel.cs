using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Conditions.History;

namespace WDE.Conditions.ViewModels
{
    public class ConditionGroupsEditorViewModel: BindableBase, IDocument
    {
        private readonly ConditionGroupsEditorHistoryHandler historyHandler;
        private readonly IConditionDataProvider conditionDataProvider;
        private readonly IWindowManager windowManager;
        private readonly IMessageBoxService messageBoxService;
        
        public ConditionGroupsEditorViewModel(Func<IHistoryManager> historyCreator, IConditionDataProvider conditionDataProvider, IWindowManager windowManager, 
            IMessageBoxService messageBoxService, ITaskRunner taskRunner)
        {
            this.conditionDataProvider = conditionDataProvider;
            this.windowManager = windowManager;
            this.messageBoxService = messageBoxService;

            MakeItems();
            
            Save = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Saving condition groups", SaveGroupsToFile);
            }, () => IsModified);
            AddGroup = new DelegateCommand(AddGroupToSource);
            DeleteItem = new DelegateCommand<object>(DeleteItemFromSource);
            AddMember = new DelegateCommand<ConditionGroupsEditorData>(AddItemToGroup);
            EditItem = new DelegateCommand<ConditionGroupsEditorData>(EditSourceItem);
            // history setup
            historyHandler = new ConditionGroupsEditorHistoryHandler(SourceItems);
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
        
        public ObservableCollection<ConditionGroupsEditorData> SourceItems { get; private set; }

        public DelegateCommand AddGroup { get; }
        public DelegateCommand<object> DeleteItem { get; }
        public DelegateCommand<ConditionGroupsEditorData> AddMember { get; }
        public DelegateCommand<ConditionGroupsEditorData> EditItem { get; }
        private readonly DelegateCommand undoCommand;
        private readonly DelegateCommand redoCommand;
        
        public void AddGroupToSource()
        {
            var result = "";
            OpenNameEditingWindow("", out result);
            if (!string.IsNullOrWhiteSpace(result))
                SourceItems.Add(new ConditionGroupsEditorData(result));
        }
        private void DeleteItemFromSource(object obj)
        {
            if (obj is ConditionGroupsEditorData source)
                SourceItems.Remove(source);
            else if (obj is ConditionGroupsEditorDataNode node)
            {
                node.Owner.Members.Remove(node);
                node.Owner = null;
            }
        }

        private void AddItemToGroup(ConditionGroupsEditorData data)
        {
            var result = "";
            OpenNameEditingWindow("CONDITION_", out result);
            if (!string.IsNullOrWhiteSpace(result))
                data.Members.Add(new ConditionGroupsEditorDataNode(result, data));
        }

        private void EditSourceItem(ConditionGroupsEditorData data)
        {
            var result = data.Name;
            OpenNameEditingWindow(data.Name, out result);
            if (!string.IsNullOrEmpty(result))
                data.Name = result;
        }

        private void OpenNameEditingWindow(string source, out string name)
        {
            var vm = new ConditionGroupsInputViewModel(source);
            name = "";
            if (windowManager.ShowDialog(vm))
            {
                if (!string.IsNullOrWhiteSpace(vm.Name))
                    name = vm.Name;
                else
                {
                    messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                        .SetMainInstruction($"Group name cannot be empty!")
                        .SetIcon(MessageBoxIcon.Error)
                        .WithOkButton(true)
                        .Build());
                }
            }
        }

        private async Task SaveGroupsToFile()
        {
            await conditionDataProvider.SaveConditionGroups(SourceItems.Select(x => x.ToConditionGroupsJsonData()).ToList());
            History.MarkAsSaved();
        }

        private void MakeItems()
        {
            SourceItems = new ObservableCollection<ConditionGroupsEditorData>();

            foreach (var item in conditionDataProvider.GetConditionGroups())
                SourceItems.Add(new ConditionGroupsEditorData(in item));
        }
        
        
        public void Dispose()
        {
            historyHandler.Dispose();
        }

        public string Title { get; } = "Condition Groups Editor";
        public ICommand Undo  => undoCommand;
        public ICommand Redo => redoCommand;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public ICommand CloseCommand { get; set; } = null;
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
    
    public class ConditionGroupsEditorData: INotifyPropertyChanged
    {
        private string name;
        public ObservableCollection<ConditionGroupsEditorDataNode> Members { get; set; }
        public event Action<ConditionGroupsEditorData, string, string> OnNameChanged = delegate {  };

        public ConditionGroupsEditorData(in ConditionGroupsJsonData source)
        {
            name = source.Name;
            if (source.Members != null)
            {
                Members = new ObservableCollection<ConditionGroupsEditorDataNode>();
                foreach (var member in source.Members)
                    Members.Add(new ConditionGroupsEditorDataNode(member, this));
            }
            else
                Members = new ObservableCollection<ConditionGroupsEditorDataNode>();
        }

        public ConditionGroupsEditorData(string name)
        {
            this.name = name;
            Members = new ObservableCollection<ConditionGroupsEditorDataNode>();
        }

        public string Name
        {
            get => name;
            set
            {
                OnNameChanged.Invoke(this, value, name);
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ConditionGroupsJsonData ToConditionGroupsJsonData()
        {
            var obj = new ConditionGroupsJsonData();
            obj.Name = Name;
            if (Members.Count > 0)
                obj.Members = Members.Select(x => x.Name).ToList();
            return obj;
        }
    }

    public class ConditionGroupsEditorDataNode
    {
        public string Name { get; set; }
        public ConditionGroupsEditorData Owner;

        public ConditionGroupsEditorDataNode(string name, ConditionGroupsEditorData owner)
        {
            Name = name;
            Owner = owner;
        }
    }
}