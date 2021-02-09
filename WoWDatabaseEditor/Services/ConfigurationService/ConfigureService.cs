using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.ConfigurationService.ViewModels;

namespace WoWDatabaseEditorCore.Services.ConfigurationService
{
    [AutoRegister]
    [SingleInstance]
    public class ConfigureService : IConfigureService
    {
        private readonly IDocumentManager documentManager;
        private readonly Func<ConfigurationPanelViewModel> settings;

        private ConfigurationPanelViewModel? openedPanel = null;

        public ConfigureService(IDocumentManager documentManager, Func<ConfigurationPanelViewModel> settings)
        {
            this.documentManager = documentManager;
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
            
            documentManager.OpenDocument(openedPanel);
        }
    }
}