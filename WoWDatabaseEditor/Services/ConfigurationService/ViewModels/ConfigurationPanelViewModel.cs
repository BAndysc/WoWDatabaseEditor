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
using WDE.Common.Utils;

namespace WoWDatabaseEditor.Services.ConfigurationService.ViewModels
{
    public class ConfigurationPanelViewModel : BindableBase, IDocument
    {
        public ConfigurationPanelViewModel(Func<IEnumerable<IConfigurable>> configs)
        {
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
        public IConfigurable? SelectedTabItem { get; set; }

        private void SaveAll()
        {
            foreach (var tab in ContainerTabItems)
            {
                if (tab.IsModified)
                    tab.Save.Execute(null);
            }
        }

        public void Dispose()
        {
            
        }

        public string Title => "Settings";
        public ICommand Save { get; }
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => ContainerTabItems.Any(t => t.IsModified);
        public IHistoryManager? History => null;
    }
}