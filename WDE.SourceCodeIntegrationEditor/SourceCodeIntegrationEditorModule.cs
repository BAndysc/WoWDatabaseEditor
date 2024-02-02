using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.Statistics;
using WDE.Common.Utils;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.SourceCode;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration;
using WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.ViewModels;
using SourceCodeConfigurationViewModel = WDE.SourceCodeIntegrationEditor.Settings.SourceCodeConfigurationViewModel;

namespace WDE.SourceCodeIntegrationEditor
{
    [AutoRegister]
    public class SourceCodeIntegrationEditorModule : ModuleBase
    {
        private VisualStudioManagerViewModel? manager;

        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);

            containerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() =>
                    {
                        manager = containerProvider.Resolve<VisualStudioManagerViewModel>();
                        async Task CheckAndAsk()
                        {
                            var statisticsService = containerProvider.Resolve<IStatisticsService>();
                            var teachingTipsService = containerProvider.Resolve<ITeachingTipService>();
                            var sourceCodePathService = containerProvider.Resolve<ISourceCodePathService>();

                            if (sourceCodePathService.SourceCodePaths.Count == 0 &&
                                statisticsService.RunCounter > 20 &&
                                teachingTipsService.ShowTip("source_code_integration"))
                            {
                                var messageBoxService = containerProvider.Resolve<IMessageBoxService>();
                                var configureService = containerProvider.Resolve<IConfigureService>();

                                await Task.Delay(TimeSpan.FromSeconds(5));
                                if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                                        .SetTitle("Source code integration")
                                        .SetMainInstruction("Provide the server sources path")
                                        .SetContent("The editor can now search in the source code in Find Anywhere. This is very useful as some things are hardcoded in the code.\n\nDo you want to open settings to provide the path to the sources?")
                                        .WithYesButton(true)
                                        .WithNoButton(false, EscapeToClose.style)
                                        .Build()))
                                    configureService.ShowSettings<SourceCodeConfigurationViewModel>();
                            }
                        }

                        CheckAndAsk().ListenErrors();
                    },
                    ThreadOption.PublisherThread,
                    true);

        }
    }
}