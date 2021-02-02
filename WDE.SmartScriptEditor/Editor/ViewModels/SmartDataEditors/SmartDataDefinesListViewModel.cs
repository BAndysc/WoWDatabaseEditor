using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.Views;
using WDE.SmartScriptEditor.Providers;
using System.Linq;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartDataDefinesListViewModel : BindableBase, IDocument
    {
        private readonly ISmartRawDataProvider smartDataProvider;
        private readonly IParameterFactory parameterFactory;
        private readonly SmartDataSourceMode dataSourceMode;
        private readonly ISmartDataManager smartDataManager;
        private readonly IMessageBoxService messageBoxService;
        private readonly IWindowManager windowManager;

        public SmartDataDefinesListViewModel(ISmartRawDataProvider smartDataProvider, ISmartDataManager smartDataManager, IParameterFactory parameterFactory,
            ITaskRunner taskRunner, IMessageBoxService messageBoxService, IWindowManager windowManager, SmartDataSourceMode dataSourceMode)
        {
            this.smartDataProvider = smartDataProvider;
            this.parameterFactory = parameterFactory;
            this.smartDataManager = smartDataManager;
            this.dataSourceMode = dataSourceMode;
            this.messageBoxService = messageBoxService;
            this.windowManager = windowManager;
            switch (dataSourceMode)
            {
                case SmartDataSourceMode.SD_SOURCE_EVENTS:
                    DefinesItems = new ObservableCollection<SmartGenericJsonData>(smartDataProvider.GetEvents());
                    break;
                case SmartDataSourceMode.SD_SOURCE_ACTIONS:
                    DefinesItems = new ObservableCollection<SmartGenericJsonData>(smartDataProvider.GetActions());
                    break;
                case SmartDataSourceMode.SD_SOURCE_TARGETS:
                    DefinesItems = new ObservableCollection<SmartGenericJsonData>(smartDataProvider.GetTargets());
                    break;
                default:
                    DefinesItems = new ObservableCollection<SmartGenericJsonData>();
                    break;
            }
            OnItemSelected = new DelegateCommand<SmartGenericJsonData?>(ShowEditorWindow);
            CreateNew = new DelegateCommand(CreateNewItem);
            DeleteItem = new DelegateCommand(DeleteSelectedItem);
            Save = new DelegateCommand(() =>
            {
                IsModified = false;
                taskRunner.ScheduleTask("Saving modified SmartData defines", SaveDataToFile);
            }, () => IsModified);
            SelectedItemIndex = -1;
        }

        public int SelectedItemIndex { get; set; }
        public DelegateCommand<SmartGenericJsonData?> OnItemSelected { get; }
        public DelegateCommand CreateNew { get; }
        public DelegateCommand DeleteItem { get; }

        public ObservableCollection<SmartGenericJsonData> DefinesItems { get; }

        private void ShowEditorWindow(SmartGenericJsonData? action)
        {
            if (action.HasValue)
                OpenEditor(action.Value, false);
        }

        private void CreateNewItem()
        {
            var newItem = new SmartGenericJsonData();
            newItem.Id = DefinesItems.Max(x => x.Id) + 1;
            OpenEditor(newItem, true);
        }

        private void DeleteSelectedItem()
        {
            if (SelectedItemIndex >= 0)
            {
                DefinesItems.RemoveAt(SelectedItemIndex);
                IsModified = true;
            }
        }

        private void OpenEditor(SmartGenericJsonData item, bool isCreating)
        {
            var vm = GetEditorViewModel(in item, isCreating);
            if (windowManager.ShowDialog(vm as IDialog) && !vm.IsSourceEmpty())
            {
                IsModified = true;
                if (vm.InsertOnSave)
                    DefinesItems.Add(vm.GetSource());
                else
                {
                    var newItem = vm.GetSource();
                    var index = DefinesItems.IndexOf(item);
                    if (index != -1)
                        DefinesItems[index] = newItem;
                }
            }
        }

        private async Task SaveDataToFile()
        {
            string infoSourceName = "";
            switch (dataSourceMode)
            {
                case SmartDataSourceMode.SD_SOURCE_EVENTS:
                    await smartDataProvider.SaveEvents(DefinesItems.ToList());
                    smartDataManager.Reload(SmartType.SmartEvent);
                    infoSourceName = "Events";
                    break;
                case SmartDataSourceMode.SD_SOURCE_ACTIONS:
                    await smartDataProvider.SaveActions(DefinesItems.ToList());
                    smartDataManager.Reload(SmartType.SmartAction);
                    infoSourceName = "Actions";
                    break;
                case SmartDataSourceMode.SD_SOURCE_TARGETS:
                    await smartDataProvider.SaveTargets(DefinesItems.ToList());
                    smartDataManager.Reload(SmartType.SmartTarget);
                    infoSourceName = "Targets";
                    break;
            }

            messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Success!")
                                    .SetMainInstruction($"Editor successfully saved definitions of Smart {infoSourceName}! Also remember to modify SmartData Group file via Editor if you modified list!")
                                    .SetIcon(MessageBoxIcon.Information)
                                    .WithOkButton(true)
                                    .Build());
        }

        private ISmartDataEditorModel GetEditorViewModel(in SmartGenericJsonData source, bool isCreating)
        {
            switch (dataSourceMode)
            {
                case SmartDataSourceMode.SD_SOURCE_EVENTS:
                    return new SmartDataEventsEditorViewModel(parameterFactory, windowManager, in source, isCreating);
                case SmartDataSourceMode.SD_SOURCE_ACTIONS:
                    return new SmartDataActionsEditorViewModel(parameterFactory, windowManager, in source, isCreating);
                case SmartDataSourceMode.SD_SOURCE_TARGETS:
                    return new SmartDataTargetsEditorViewModel(parameterFactory, windowManager, in source, isCreating);
            }
            return null;
        }

        public string Title => "Smart Data Editor";
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save { get; private set; }
        public ICommand CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public IHistoryManager History => null;
        private bool isModified = false;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }

        public void Dispose()
        {
            
        }
    }
}
