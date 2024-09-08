using System;
using System.Linq;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.ConfigurationService.ViewModels;

namespace WoWDatabaseEditorCore.Services.ConfigurationService
{
    [AutoRegister]
    [SingleInstance]
    public class ConfigureService : IConfigureService
    {
        private class Either<TA, TB>
        {
            private TA? a;
            private TB? b;

            private bool hasA;
            private bool hasB;

            public Either(TA x)
            {
                a = x;
                hasA = true;
            }

            public Either(TB x)
            {
                b = x;
                hasB = true;
            }

            public Either()
            {
            }

            public bool TryGet<T>(out T ret)
            {
                if (hasA && typeof(T) == typeof(TA))
                {
                    ret = (T)(object)a!;
                    return true;
                }
                if (hasB && typeof(T) == typeof(TB))
                {
                    ret = (T)(object)b!;
                    return true;
                }

                ret = default!;
                return false;
            }
        }

        private readonly IDocumentManager documentManager;
        private readonly Func<ConfigurationPanelViewModel> settings;

        private ConfigurationPanelViewModel? openedPanel = null;

        public ConfigureService(IDocumentManager documentManager, Func<ConfigurationPanelViewModel> settings)
        {
            this.documentManager = documentManager;
            this.settings = settings;
        }

        private object? ShowSettings(Either<Type, IConfigurable> toOpen)
        {
            if (openedPanel == null)
            {
                openedPanel = settings();
                IAsyncCommand? origCommand = openedPanel.CloseCommand;
                openedPanel.CloseCommand = new AsyncCommand(async () =>
                {
                    if (origCommand != null)
                        await origCommand.ExecuteAsync();
                    openedPanel = null;
                });
            }

            Func<IConfigurable, bool> configMatcher = config =>
            {
                if (toOpen.TryGet<Type>(out var type))
                    return config.GetType() == type;
                if (toOpen.TryGet<IConfigurable>(out var configToFind))
                    return config.GetType() == configToFind.GetType();

                return false;
            };

            object? configurablePanel = null;
            if (openedPanel.ContainerTabItems.FirstOrDefault(configMatcher) is { } configurable)
            {
                configurablePanel = configurable;
                openedPanel.SelectedTabItem = configurable;
            }
            
            documentManager.OpenDocument(openedPanel);
            return configurablePanel;
        }

        public void ShowSettings() => ShowSettings(new Either<Type, IConfigurable>());
        
        public T? ShowSettings<T>() where T : IConfigurable => (T?)ShowSettings(new Either<Type, IConfigurable>(typeof(T)));

        public void ShowSettings(IConfigurable configurable) => ShowSettings(new Either<Type, IConfigurable>(configurable));
    }
}