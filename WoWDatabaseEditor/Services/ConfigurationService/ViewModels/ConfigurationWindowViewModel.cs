using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

using WDE.Common;
using Prism.Ioc;

namespace WoWDatabaseEditor.Services.ConfigurationService.ViewModels
{
    public class ConfigurationWindowViewModel : BindableBase
    {
        public class ContainerTab
        {
            public string Header { get; set; }
            public ContentControl Control { get; set; }
            public Action Save { get; set; }
        }

        public ObservableCollection<ContainerTab> ContainerTabItems { get; set; }
        public ContainerTab SelectedTabItem { get; set; }
        public DelegateCommand SaveAction { get; private set; }

        public ConfigurationWindowViewModel(IEnumerable<IConfigurable> configs)
        {
            ContainerTabItems = new ObservableCollection<ContainerTab>();

            foreach (var config in configs)
            {
                var view = config.GetConfigurationView();
                ContainerTabItems.Add(new ContainerTab()
                {
                    Header = config.GetName(),
                    Control = view.Key,
                    Save = view.Value
                });
            }

            SaveAction = new DelegateCommand(Save);
        }

        private void Save()
        {
            SelectedTabItem.Save();
        }
    }
}
