using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;

namespace WoWDatabaseEditor.Services.ConfigurationService.ViewModels
{
    public class ConfigurationWindowViewModel : BindableBase
    {
        public ConfigurationWindowViewModel(IEnumerable<IConfigurable> configs)
        {
            ContainerTabItems = new ObservableCollection<ContainerTab>();

            foreach (var config in configs)
            {
                var view = config.GetConfigurationView();
                ContainerTabItems.Add(new ContainerTab(config.GetName(), view.Key, view.Value));
            }

            SaveAction = new DelegateCommand(Save);
        }

        public ObservableCollection<ContainerTab> ContainerTabItems { get; set; }
        public ContainerTab? SelectedTabItem { get; set; }
        public DelegateCommand SaveAction { get; }

        private void Save()
        {
            if (SelectedTabItem != null)
                SelectedTabItem.Save();
        }

        public class ContainerTab
        {
            public ContainerTab(string header, ContentControl control, Action save)
            {
                Header = header;
                Control = control;
                Save = save;
            }

            public string Header { get; }
            public ContentControl Control { get; }
            public Action Save { get; }
        }
    }
}