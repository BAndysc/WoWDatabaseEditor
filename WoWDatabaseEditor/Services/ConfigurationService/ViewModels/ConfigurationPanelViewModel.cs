using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WoWDatabaseEditorCore.Extensions;

namespace WoWDatabaseEditorCore.Services.ConfigurationService.ViewModels
{
    public class ConfigurationPanelViewModel : BindableBase, IDocument
    {
        private readonly IMessageBoxService messageBoxService;

        public ConfigurationPanelViewModel(Func<IEnumerable<IConfigurable>> configs, IMessageBoxService messageBoxService)
        {
            this.messageBoxService = messageBoxService;
            ContainerTabItems = new ObservableCollection<IConfigurable>();

            ContainerTabItems.AddRange(configs());

            foreach (var tab in ContainerTabItems)
            {
                tab.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(tab.IsModified))
                        RaisePropertyChanged(nameof(IsModified));
                };
            }

            if (ContainerTabItems.Count > 0)
                SelectedTabItem = ContainerTabItems[0];
            
            Save = new DelegateCommand(SaveAll);
        }

        public ObservableCollection<IConfigurable> ContainerTabItems { get; set; }
        private IConfigurable? selectedTabItem;
        public IConfigurable? SelectedTabItem
        {
            get => selectedTabItem;
            set
            {
                SetProperty(ref selectedTabItem, value);
                value?.ConfigurableOpened();
            }
        }

        private void SaveAll()
        {
            bool restartRequired = false;
            foreach (var tab in ContainerTabItems)
            {
                if (tab.IsModified)
                {
                    tab.Save.Execute(null);
                    restartRequired |= tab.IsRestartRequired;
                }
            }

            if (restartRequired)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Settings updated")
                    .SetMainInstruction("Restart is required")
                    .SetContent("To apply new settings, you have to restart the application")
                    .SetIcon(MessageBoxIcon.Information)
                    .WithOkButton(true)
                    .Build());
            }
        }

        public void Dispose()
        {
            foreach (var tab in ContainerTabItems)
            {
                if (tab is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public string Title => "Settings";
        public ICommand Save { get; }
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public AsyncAwaitBestPractices.MVVM.IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => ContainerTabItems.Any(t => t.IsModified);
        public IHistoryManager? History => null;
    }
}