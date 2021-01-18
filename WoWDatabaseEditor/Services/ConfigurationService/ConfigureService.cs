using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditor.Services.ConfigurationService.ViewModels;

namespace WoWDatabaseEditor.Services.ConfigurationService
{
    [AutoRegister]
    [SingleInstance]
    public class ConfigureService : IConfigureService
    {
        private readonly IWindowManager windowManager;
        private readonly Func<ConfigurationPanelViewModel> settings;

        private ConfigurationPanelViewModel? openedPanel = null;

        public ConfigureService(IWindowManager windowManager, Func<ConfigurationPanelViewModel> settings)
        {
            this.windowManager = windowManager;
            this.settings = settings;
        }

        public void ShowSettings()
        {
            if (openedPanel == null)
            {
                openedPanel = settings();
                ICommand? origCommand = openedPanel.CloseCommand;
                openedPanel.CloseCommand = new DelegateCommand(() =>
                {
                    origCommand?.Execute(null);
                    openedPanel = null;
                });
            }
            
            windowManager.OpenDocument(openedPanel);
        }
    }
}