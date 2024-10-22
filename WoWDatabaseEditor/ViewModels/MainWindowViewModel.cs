using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using ReactiveUI;
using WDE.Common.CoreVersion;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Windows;
using WDE.Common.Menu;
using WDE.Common.Providers;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Services.Statistics;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditor.Providers;
using WoWDatabaseEditorCore.Managers;
using WoWDatabaseEditorCore.Services;
using WoWDatabaseEditorCore.Services.FindAnywhere;
using WoWDatabaseEditorCore.Services.Profiles;
using WoWDatabaseEditorCore.Services.QuickAccess;
using WoWDatabaseEditorCore.Services.Statistics;

namespace WoWDatabaseEditorCore.ViewModels
{
    [SingleInstance]
    [AutoRegister]
    public partial class MainWindowViewModel : ObservableBase, ILayoutViewModelResolver, ICloseAwareViewModel
    {
        private readonly IStatusBar statusBar;
        private readonly IMessageBoxService messageBoxService;
        private readonly Func<AboutViewModel> aboutViewModelCreator;
        private readonly Func<QuickStartViewModel> quickStartCreator;
        private readonly Func<TextDocumentViewModel> textDocumentCreator;
        private readonly ISolutionTasksService solutionTasksService;
        private readonly IProgramNameService programNameService;
        private readonly List<IProgramNameAddon> programNameAddons;
        private readonly ITablesToolService tablesToolService;
        private readonly IGlobalServiceRoot globalServiceRoot;
        private readonly ITeachingTipService teachingTipService;
        private readonly IStatisticsService statisticsService;
        private readonly IUpdateViewModel updateViewModel;
        private readonly IClippy clippy;

        private readonly Dictionary<string, ITool> toolById = new();

        [Notify] private bool is3dViewSupported;

        public MainWindowViewModel(IDocumentManager documentManager,
            StatusBarViewModel statusBarViewModel,
            IStatusBar statusBar,
            IMessageBoxService messageBoxService,
            TasksViewModel tasksViewModel,
            EditorMainMenuItemsProvider menuItemProvider,
            RelatedSolutionItems relatedSolutionItems,
            ISolutionSqlService solutionSqlService,
            Func<AboutViewModel> aboutViewModelCreator,
            Func<QuickStartViewModel> quickStartCreator,
            Func<TextDocumentViewModel> textDocumentCreator,
            ISolutionTasksService solutionTasksService,
            ISolutionItemSqlGeneratorRegistry queryGeneratorRegistry,
            IClipboardService clipboardService,
            ISessionService sessionService,
            ITaskRunner taskRunner,
            IEventAggregator eventAggregator,
            IProgramNameService programNameService,
            IEnumerable<IProgramNameAddon> nameAddons,
            IMainThread mainThread,
            IQuickAccessViewModel quickAccessViewModel,
            IWindowManager windowManager,
            ITablesToolService tablesToolService,
            Lazy<IGameViewService> gameViewService,
            Lazy<ISqlEditorService> sqlEditorService,
            QuickGoToViewModel quickGoToViewModel,
            ProfilesViewModel profilesViewModel,
            IGlobalServiceRoot globalServiceRoot,
            TopBarQuickAccessViewModel topBarQuickAccessViewModel,
            SessionRestoreService sessionRestoreService,
            ITeachingTipService teachingTipService,
            IStatisticsService statisticsService,
            ICurrentCoreVersion currentCoreVersion,
            IUpdateViewModel updateViewModel,
            IClippy clippy,
            IVisualStudioManagerViewModel visualStudioManagerViewModel,
            IEnumerable<IVersionedFilesViewModel> versionedFilesViewModel,
            Func<IFindAnywhereDialogViewModel> findAnywhereCreator)
        {
            DocumentManager = documentManager;
            StatusBar = statusBarViewModel;
            TopBarQuickAccess = topBarQuickAccessViewModel;
            SessionRestoreService = sessionRestoreService;
            VisualStudioManagerViewModel = visualStudioManagerViewModel;
            VersionedFilesViewModel = versionedFilesViewModel.FirstOrDefault();
            this.statusBar = statusBar;
            this.messageBoxService = messageBoxService;
            this.aboutViewModelCreator = aboutViewModelCreator;
            this.quickStartCreator = quickStartCreator;
            this.textDocumentCreator = textDocumentCreator;
            this.solutionTasksService = solutionTasksService;
            this.programNameService = programNameService;
            this.tablesToolService = tablesToolService;
            this.globalServiceRoot = globalServiceRoot;
            this.teachingTipService = teachingTipService;
            this.statisticsService = statisticsService;
            this.updateViewModel = updateViewModel;
            this.clippy = clippy;
            this.programNameAddons = nameAddons.ToList();
            Title = "";
            Subtitle = programNameService.Subtitle;
            ShowRelatedItems = !currentCoreVersion.Current.HideRelatedItems;
            foreach (var titleAddon in programNameAddons)
                titleAddon.ToObservable(x => x.Addon).SubscribeAction(_ => UpdateTitle());
            UpdateTitle();
            OpenDocument = new DelegateCommand<IMenuDocumentItem>(ShowDocument);
            ExecuteChangedCommand = new DelegateCommand(() =>
            {
                var item = DocumentManager.ActiveSolutionItemDocument?.SolutionItem;
                if (item == null)
                {
                    if (DocumentManager.ActiveDocument is { } doc)
                        doc.Save.Execute(null);
                    return;
                }

                solutionTasksService.Save(DocumentManager.ActiveSolutionItemDocument!).ListenErrors(this.messageBoxService);
            }, () => (DocumentManager.ActiveSolutionItemDocument != null &&
                     (solutionTasksService.CanSaveAndReloadRemotely || solutionTasksService.CanSaveToDatabase)) ||
                     (DocumentManager.ActiveDocument is {} doc && doc.Save.CanExecute(null)));
            solutionTasksService.ToObservable(x => x.CanSaveAndReloadRemotely)
                .SubscribeAction(_ => ExecuteChangedCommand.RaiseCanExecuteChanged());

            CopyCurrentSqlCommand = new AsyncAutoCommand(async () =>
            {
                if (DocumentManager.ActiveDocument is ISolutionItemDocument { SolutionItem: { } } sid)
                {
                    await taskRunner.ScheduleTask("Generating SQL",
                        async () =>
                        {
                            var sql = await queryGeneratorRegistry.GenerateSql(sid.SolutionItem);
                            clipboardService.SetText(sql.QueryString);
                            this.statusBar.PublishNotification(new PlainNotification(NotificationType.Success, "SQL copied!"));
                        });
                }
            }, () => DocumentManager.ActiveDocument != null && DocumentManager.ActiveDocument is ISolutionItemDocument);
            
            GenerateCurrentSqlCommand = new DelegateCommand(() =>
            {
                if (DocumentManager.ActiveDocument is ISolutionItemDocument {SolutionItem: { }} sid)
                    solutionSqlService.OpenDocumentWithSqlFor(sid.SolutionItem);
            }, () => DocumentManager.ActiveDocument != null && DocumentManager.ActiveDocument is ISolutionItemDocument);

            FindAnywhereCommand = new AsyncAutoCommand(async () =>
            {
                await windowManager.ShowDialog(findAnywhereCreator());
            });

            Open3DCommand = new DelegateCommand(() =>
            {
                gameViewService.Value.Open();
            });

            OpenSqlDocumentCommand = new DelegateCommand(() =>
            {
                sqlEditorService.Value.NewDocument();
            });

            DocumentManager.ToObservable(dm => dm.ActiveDocument)
                .SubscribeAction(_ =>
                {
                    GenerateCurrentSqlCommand.RaiseCanExecuteChanged();
                    ExecuteChangedCommand.RaiseCanExecuteChanged();
                    CopyCurrentSqlCommand.RaiseCanExecuteChanged();
                });
            
            TasksViewModel = tasksViewModel;
            RelatedSolutionItems = relatedSolutionItems;
            QuickAccessViewModel = quickAccessViewModel;
            QuickGoToViewModel = quickGoToViewModel;
            ProfilesViewModel = profilesViewModel;

            MenuItemProviders = menuItemProvider.GetItems();

            foreach (var window in documentManager.AllTools)
                toolById[window.UniqueId] = window;

            documentManager.OpenedDocuments.ToCountChangedObservable().SubscribeAction(count =>
            {
                if (count == 0 && !inCanClose)
                    mainThread.Delay(ShowStartPage, TimeSpan.FromMilliseconds(1));
            });
            //LoadDefault();

            Watch(this.updateViewModel, x => x.HasPendingUpdate, nameof(HasUpdateToDownload));
            Watch(this.updateViewModel, x => x.InstallPendingCommandUpdate, nameof(DownloadUpdateCommand));
            Watch(DocumentManager, dm => dm.ActiveSolutionItemDocument, nameof(ShowExportButtons));
            Watch(DocumentManager, dm => dm.ActiveDocument, nameof(ShowPlayButtons));
            Watch(tablesToolService, serv => serv.Visibility, nameof(ShowTablesList));

            eventAggregator.GetEvent<AllModulesLoaded>()
                .Subscribe(() => Is3dViewSupported = gameViewService.Value.IsSupported);

            eventAggregator.GetEvent<AllModulesLoaded>()
                .Subscribe(OpenFatalLogIfExists, ThreadOption.PublisherThread, true);
        }

        public bool ShowSqlEditorNotification()
        {
            return statisticsService.RunCounter > 12 && teachingTipService.IsTipShown("GlobalSearchNotification") && teachingTipService.ShowTip("SqlEditorNotification");
        }

        public bool ShowGlobalSearchNotification()
        {
            return statisticsService.RunCounter > 11 && teachingTipService.ShowTip("GlobalSearchNotification");
        }

        private void OpenFatalLogIfExists()
        {
            if (!FatalErrorHandler.HasFatalLog())
                return;

            var log = FatalErrorHandler.ConsumeFatalLog();
            DocumentManager.OpenDocument(textDocumentCreator().Set("Crash log", log));
            
            messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("WoW Database Editor has been closed due to the fatal error")
                .SetIcon(MessageBoxIcon.Error)
                .SetMainInstruction("WoW Database Editor has been closed due to the fatal error")
                .SetContent("Sorry, the editor has been closed to the fatal error, a log with the error is now opened, you can report the bug via Help -> Report a bug and attach the log")
                .WithOkButton(true)
                .Build());
        }

        private void UpdateTitle()
        {
            if (programNameAddons.Count == 0)
                Title = programNameService.Title;
            else
            {
                var name = string.Join(" ", programNameAddons.Select(a => a.Addon));
                Title = programNameService.Title + " " + name;
            }
            RaisePropertyChanged(nameof(Title));
        }

        public StatusBarViewModel StatusBar { get; }
        public IDocumentManager DocumentManager { get; }

        public TasksViewModel TasksViewModel { get; }
        public RelatedSolutionItems RelatedSolutionItems { get; }
        public IQuickAccessViewModel QuickAccessViewModel { get; }
        public QuickGoToViewModel QuickGoToViewModel { get; }
        public ProfilesViewModel ProfilesViewModel { get; }
        public TopBarQuickAccessViewModel TopBarQuickAccess { get; }
        public SessionRestoreService SessionRestoreService { get; }
        public IVisualStudioManagerViewModel VisualStudioManagerViewModel { get; }

        public List<IMainMenuItem> MenuItemProviders { get; }

        public bool HasUpdateToDownload => updateViewModel.HasPendingUpdate;

        public string Title { get; private set; }
        
        public string Subtitle { get; }
        
        public bool ShowRelatedItems { get; }

        public DelegateCommand<IMenuDocumentItem> OpenDocument { get; }

        // this fallback to QuickStartViewModel is a hack, otherwise Avalonia for some reason will never show this button if it is not visible in the beginning
        public bool ShowPlayButtons => (DocumentManager.ActiveSolutionItemDocument?.ShowExportToolbarButtons ?? false) || (DocumentManager.ActiveDocument?.Save?.CanExecute(null) ?? false);
        
        public bool ShowExportButtons => DocumentManager.ActiveSolutionItemDocument?.ShowExportToolbarButtons ?? true;
        
        public DelegateCommand ExecuteChangedCommand { get; }
        
        public AsyncAutoCommand CopyCurrentSqlCommand { get; }
        
        public DelegateCommand GenerateCurrentSqlCommand { get; }

        public ICommand FindAnywhereCommand { get; }
        
        public ICommand Open3DCommand { get; }
        
        public ICommand OpenSqlDocumentCommand { get; }

        public ICommand DownloadUpdateCommand => updateViewModel.InstallPendingCommandUpdate;

        public IVersionedFilesViewModel? VersionedFilesViewModel { get; }

        public bool ShowTablesList
        {
            get => tablesToolService.Visibility;
            set
            {
                if (value)
                    tablesToolService.Open();
                else
                    tablesToolService.Close();
            }
        }

        private void ShowAbout()
        {
            DocumentManager.OpenDocument(aboutViewModelCreator());
        }

        private void ShowStartPage()
        {
            DocumentManager.OpenDocument(quickStartCreator());
        }
        
        private void ShowDocument(IMenuDocumentItem documentItem)
        {
            DocumentManager.OpenDocument(documentItem.EditorDocument());
        }

        public ITool? ResolveViewModel(string id)
        {
            if (toolById.TryGetValue(id, out var tool))
            {
                DocumentManager.OpenedTools.Add(tool);
                return tool;
            }

            return null;
        }

        public void Activated(bool firstTime, bool classicTheme)
        {
            if (firstTime && classicTheme && teachingTipService.ShowTip("ClassicClippy_Once"))
            {
                clippy.Open();
            }
        }

        public void LoadDefault()
        {
            foreach (var tool in toolById.Values)
            {
                if (tool.OpenOnStart)
                    DocumentManager.OpenTool(tool.GetType());
            }
        }

        private bool inCanClose;
        public async Task<bool> CanClose()
        {
            if (inCanClose)
            {
                await messageBoxService.SimpleDialog("Closing in progress", "Closing in progress",
                    "This app is already being closing (probably some async operation going in background)");
                return false;
            }

            inCanClose = true;

            try
            {
                return await DocumentManager.TryCloseAllDocuments(true);
            }
            finally
            {
                inCanClose = false;
            }
        }

        public async Task<bool> TryClose()
        {
            if (!await CanClose())
                return false;
            
            SessionRestoreService.GracefulShutdown();
            CloseRequest?.Invoke();
            return true;
        }

        public void ForceClose()
        {
            ForceCloseRequest?.Invoke();
        }

        public event Action CloseRequest = delegate{};
        public event Action ForceCloseRequest = delegate{};

        public void NotifyWillClose()
        {
            globalServiceRoot.Dispose();
        }
    }
}
