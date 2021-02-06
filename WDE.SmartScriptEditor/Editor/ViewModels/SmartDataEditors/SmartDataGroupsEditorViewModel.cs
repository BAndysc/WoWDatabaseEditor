﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.Common.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.SmartScriptEditor.Providers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.Views;
using WDE.SmartScriptEditor.History;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartDataGroupsEditorViewModel: BindableBase, IDocument
    {
        private readonly ISmartRawDataProvider smartDataProvider;
        private readonly SmartDataSourceMode dataSourceMode;
        private readonly IMessageBoxService messageBoxService;
        private readonly IWindowManager windowManager;
        private readonly SmartDataGroupsHistory historyHandler;
        
        public SmartDataGroupsEditorViewModel(ISmartRawDataProvider smartDataProvider, ITaskRunner taskRunner, IMessageBoxService messageBoxService,
            IWindowManager windowManager, Func<IHistoryManager> historyCreator, SmartDataSourceMode dataSourceMode)
        {
            this.smartDataProvider = smartDataProvider;
            this.messageBoxService = messageBoxService;
            this.windowManager = windowManager;
            this.dataSourceMode = dataSourceMode;
            MakeItems();
            AddGroup = new DelegateCommand(AddGroupToSource);
            Save = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Saving Group Definitions to file", SaveGroupsToFile);
            });
            DeleteItem = new DelegateCommand<object>(DeleteItemFromSource);
            AddMember = new DelegateCommand<SmartDataGroupsEditorData>(AddItemToGroup);
            EditItem = new DelegateCommand<SmartDataGroupsEditorData>(EditSourceItem);
            // history setup
            History = historyCreator();
            historyHandler = new SmartDataGroupsHistory(SourceItems);
            UndoCommand = new DelegateCommand(History.Undo, () => History.CanUndo);
            RedoCommand = new DelegateCommand(History.Redo, () => History.CanRedo);
            History.PropertyChanged += (sender, args) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
                IsModified = !History.IsSaved;
                RaisePropertyChanged(nameof(IsModified));
            };
            History.AddHandler(historyHandler);
        }

        public ObservableCollection<SmartDataGroupsEditorData> SourceItems { get; private set; }

        public DelegateCommand AddGroup { get; }
        public DelegateCommand<object> DeleteItem { get; }
        public DelegateCommand<SmartDataGroupsEditorData> AddMember { get; }
        public DelegateCommand<SmartDataGroupsEditorData> EditItem { get; }
        private DelegateCommand UndoCommand;
        private DelegateCommand RedoCommand;

        public void AddGroupToSource()
        {
            var result = "";
            OpenNameEditingWindow("", out result);
            if (!string.IsNullOrWhiteSpace(result))
                SourceItems.Add(new SmartDataGroupsEditorData(result));
        }
        private void DeleteItemFromSource(object obj)
        {
            if (obj is SmartDataGroupsEditorData source)
                SourceItems.Remove(source);
            else if (obj is SmartDataGroupsEditorDataNode node)
            {
                node.Owner.Members.Remove(node);
                node.Owner = null;
            }
        }

        private void AddItemToGroup(SmartDataGroupsEditorData data)
        {
            var result = "";
            OpenNameEditingWindow(GetNewItemNamePrefix(), out result);
            if (!string.IsNullOrWhiteSpace(result))
                data.Members.Add(new SmartDataGroupsEditorDataNode(result, data));
        }

        private void EditSourceItem(SmartDataGroupsEditorData data)
        {
            var result = data.Name;
            OpenNameEditingWindow(data.Name, out result);
            if (!string.IsNullOrEmpty(result))
                data.Name = result;
        }

        private void OpenNameEditingWindow(string source, out string name)
        {
            var vm = new SmartDataGroupsInputViewModel(source);
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
            string infoSourceName = "";
            switch (dataSourceMode)
            {
                case SmartDataSourceMode.SD_SOURCE_EVENTS:
                    await smartDataProvider.SaveEventGroups(SourceItems.Select(x => x.ToSmartGroupsJsonData()).ToList());
                    infoSourceName = "Events";
                    break;
                case SmartDataSourceMode.SD_SOURCE_ACTIONS:
                    await smartDataProvider.SaveActionsGroups(SourceItems.Select(x => x.ToSmartGroupsJsonData()).ToList());
                    infoSourceName = "Actions";
                    break;
                case SmartDataSourceMode.SD_SOURCE_TARGETS:
                    await smartDataProvider.SaveTargetsGroups(SourceItems.Select(x => x.ToSmartGroupsJsonData()).ToList());
                    infoSourceName = "Targets";
                    break;
            }
            History.MarkAsSaved();
            messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Success!")
                                    .SetMainInstruction($"Editor successfully saved definitions of {infoSourceName} Groups!")
                                    .SetIcon(MessageBoxIcon.Information)
                                    .WithOkButton(true)
                                    .Build());
        }

        private void MakeItems()
        {
            SourceItems = new ObservableCollection<SmartDataGroupsEditorData>();
            List<SmartGroupsJsonData> source;
            switch (dataSourceMode)
            {
                case SmartDataSourceMode.SD_SOURCE_EVENTS:
                    source = smartDataProvider.GetEventsGroups().ToList();
                    break;
                case SmartDataSourceMode.SD_SOURCE_ACTIONS:
                    source = smartDataProvider.GetActionsGroups().ToList();
                    break;
                case SmartDataSourceMode.SD_SOURCE_TARGETS:
                    source = smartDataProvider.GetTargetsGroups().ToList();
                    break;
                default:
                    source = smartDataProvider.GetEventsGroups().ToList();
                    break;
            }

            foreach (var item in source)
                SourceItems.Add(new SmartDataGroupsEditorData(in item));
        }

        private string GetNewItemNamePrefix()
        {
            switch (dataSourceMode)
            {
                case SmartDataSourceMode.SD_SOURCE_EVENTS:
                    return "SMART_EVENT_";
                case SmartDataSourceMode.SD_SOURCE_ACTIONS:
                    return "SMART_ACTION_";
                case SmartDataSourceMode.SD_SOURCE_TARGETS:
                    return "SMART_TARGET_";
                default:
                    return "";
            }
        }

        public string Title => "SmartData Groups Editor";
        public ICommand Undo => UndoCommand;
        public ICommand Redo => RedoCommand;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save { get; private set; }
        public ICommand CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public IHistoryManager History { get; }
        private bool isModified = false;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }

        public void Dispose()
        {
            historyHandler.Dispose();
        }
    }

    public class SmartDataGroupsEditorData: INotifyPropertyChanged
    {
        private string name;
        public ObservableCollection<SmartDataGroupsEditorDataNode> Members { get; set; }
        public event Action<SmartDataGroupsEditorData, string, string> OnNameChanged = delegate {  };

        public SmartDataGroupsEditorData(in SmartGroupsJsonData source)
        {
            name = source.Name;
            if (source.Members != null)
            {
                Members = new ObservableCollection<SmartDataGroupsEditorDataNode>();
                foreach (var member in source.Members)
                    Members.Add(new SmartDataGroupsEditorDataNode(member, this));
            }
            else
                Members = new ObservableCollection<SmartDataGroupsEditorDataNode>();
        }

        public SmartDataGroupsEditorData(string name)
        {
            this.name = name;
            Members = new ObservableCollection<SmartDataGroupsEditorDataNode>();
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

        public SmartGroupsJsonData ToSmartGroupsJsonData()
        {
            var obj = new SmartGroupsJsonData();
            obj.Name = Name;
            if (Members.Count > 0)
                obj.Members = Members.Select(x => x.Name).ToList();
            return obj;
        }
    }

    public class SmartDataGroupsEditorDataNode
    {
        public string Name { get; set; }
        public SmartDataGroupsEditorData Owner;

        public SmartDataGroupsEditorDataNode(string name, SmartDataGroupsEditorData owner)
        {
            Name = name;
            Owner = owner;
        }
    }
}
