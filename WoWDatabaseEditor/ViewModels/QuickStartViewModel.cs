using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Documents;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.CoreVersion;
using WoWDatabaseEditorCore.Extensions;
using WoWDatabaseEditorCore.Services;
using WoWDatabaseEditorCore.Services.DotNetUtils;
using WoWDatabaseEditorCore.Services.Http;
using WoWDatabaseEditorCore.Services.NewItemService;
using WoWDatabaseEditorCore.Services.Statistics;

namespace WoWDatabaseEditorCore.ViewModels
{
    public class QuickStartViewModel : ObservableBase, IDocument
    {
        private readonly ISolutionItemIconRegistry iconRegistry;
        private readonly ISolutionItemNameRegistry nameRegistry;
        private readonly IMostRecentlyUsedService mostRecentlyUsedService;
        private readonly IDotNetService dotNetService;
        private bool showGiveStarBox;
        public AboutViewModel AboutViewModel { get; }
        public ObservableCollection<NewItemPrototypeInfo> FlatItemPrototypes { get; } = new();
        public ObservableCollection<MostRecentlyUsedViewModel> MostRecentlyUsedItems { get; } = new();
        public ObservableCollection<IWizardProvider> Wizards { get; } = new();
        public bool HasWizards { get; }

        public ICommand DismissCommand { get; set; }
        public ICommand OpenGithubAndDismissCommand { get; set; }
        
        public bool ShowGiveStarBox
        {
            get => showGiveStarBox;
            set => SetProperty(ref showGiveStarBox, value);
        }

        public bool IsWindows7
        {
            get
            {
                var os = System.Environment.OSVersion;
                return os.Platform == PlatformID.Win32NT && os.Version.Major <= 6 && os.Version.Minor <= 1;
            }
        }

        public string ProgramTitle { get; }
        
        public QuickStartViewModel(ISolutionItemProvideService solutionItemProvideService, 
            IEnumerable<IWizardProvider> wizards,
            IEventAggregator eventAggregator,
            ISolutionItemIconRegistry iconRegistry,
            ISolutionItemNameRegistry nameRegistry,
            ICurrentCoreVersion currentCoreVersion,
            IMainThread mainThread,
            IMostRecentlyUsedService mostRecentlyUsedService,
            IDocumentManager documentManager,
            IParameterFactory parameterFactory,
            IUserSettings userSettings,
            IStatisticsService statisticsService,
            IApplicationReleaseConfiguration applicationReleaseConfiguration,
            IUrlOpenService urlOpenService,
            IDotNetService dotNetService,
            IWindowManager windowManager,
            IProgramNameService programNameService,
            AboutViewModel aboutViewModel)
        {
            this.iconRegistry = iconRegistry;
            this.nameRegistry = nameRegistry;
            this.mostRecentlyUsedService = mostRecentlyUsedService;
            this.dotNetService = dotNetService;
            ProgramTitle = programNameService.Title;
            Wizards.AddRange(wizards.Where(w => w.IsCompatibleWithCore(currentCoreVersion.Current)));
            HasWizards = Wizards.Count > 0;
            AboutViewModel = aboutViewModel;
            
            foreach (var item in solutionItemProvideService.All)
            {
                if (item.IsContainer || !item.ShowInQuickStart(currentCoreVersion.Current))
                    continue;

                bool enabled = true;

                if (!item.IsCompatibleWithCore(currentCoreVersion.Current))
                {
                    if (currentCoreVersion.IsSpecified)
                        continue;
                    else
                        enabled = false;
                }
                
                var info = new NewItemPrototypeInfo(item, enabled);

                if (info.RequiresName)
                    continue;
                
                FlatItemPrototypes.Add(info);
            }

            LoadItemCommand = new AsyncAutoCommand<NewItemPrototypeInfo>(async prototype =>
            {
                var item = await prototype.CreateSolutionItem("");
                if (item != null)
                    eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
            });

            OpenMostRecentlyUsedCommand = new AsyncAutoCommand<MostRecentlyUsedViewModel>(async item =>
            {
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item.Item);
            });

            LoadWizard = new AsyncAutoCommand<IWizardProvider>(async item =>
            {
                var wizard = await item.Create();
                documentManager.OpenDocument(wizard);
            });

            DismissCommand = new DelegateCommand(() =>
            {
                ShowGiveStarBox = false;
                userSettings.Update(new QuickStartSettings(){DismissedLeaveStarBox = true});
            });

            OpenGithubAndDismissCommand = new DelegateCommand(() =>
            {
                urlOpenService.OpenUrl("https://github.com/BAndysc/WoWDatabaseEditor");
                DismissCommand.Execute(null);
            });
            
            parameterFactory.OnRegister().SubscribeAction(_ =>
            {
                ReloadMruList();
            });
            
            AutoDispose(eventAggregator.GetEvent<EventRequestOpenItem>().Subscribe(item =>
            {
                mainThread.Dispatch(ReloadMruList);
            }, true));

            OpenDotNet6Website = new AsyncAutoCommand(async () =>
            {
                var url = dotNetService.DownloadDotNet6Link;
                windowManager.OpenUrl(url.ToString());
            });

            CheckDotNet().ListenErrors();
            
            ShowGiveStarBox = statisticsService.RunCounter > 20 &&
                              !applicationReleaseConfiguration.GetBool("SKIP_STAR_BOX").GetValueOrDefault() &&
                              !userSettings.Get<QuickStartSettings>().DismissedLeaveStarBox;

            ReloadMruList();
        }

        private async Task CheckDotNet()
        {
            var isInstalled = await dotNetService.IsDotNet6Installed();
            IsDotNet6Installed = isInstalled;
            RaisePropertyChanged(nameof(IsDotNet6Installed));
        }
        
        private void ReloadMruList()
        {
            MostRecentlyUsedItems.Clear();
            foreach (var mru in mostRecentlyUsedService.MostRecentlyUsed)
            {
                var name = nameRegistry.GetName(mru);
                if (!string.IsNullOrEmpty(mru.ExtraId))
                    name += $" ({mru.ExtraId})";
                var vm = new MostRecentlyUsedViewModel(iconRegistry.GetIcon(mru), name, mru);
                MostRecentlyUsedItems.Add(vm);
            }
        }

        public bool IsDotNet6Installed { get; private set; } = true;
        public AsyncAutoCommand OpenDotNet6Website { get; }
        public AsyncAutoCommand<NewItemPrototypeInfo> LoadItemCommand { get; }
        public AsyncAutoCommand<IWizardProvider> LoadWizard { get; }
        public AsyncAutoCommand<MostRecentlyUsedViewModel> OpenMostRecentlyUsedCommand { get; }
        
        public ImageUri? Icon => new ImageUri("Icons/wde_icon.png");
        public string Title => "Quick start";
        public ICommand Undo => AlwaysDisabledCommand.Command;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose => true;
        public bool IsModified => false;
        public IHistoryManager? History => null;
    }

    public struct QuickStartSettings : ISettings
    {
        public bool DismissedLeaveStarBox { get; set; }
    }

    public class MostRecentlyUsedViewModel
    {
        public ImageUri Icon { get; }
        public string Name { get; }
        public ISolutionItem Item { get; }

        public MostRecentlyUsedViewModel(ImageUri icon, string name, ISolutionItem item)
        {
            Icon = icon;
            Name = name;
            Item = item;
        }
    }
}