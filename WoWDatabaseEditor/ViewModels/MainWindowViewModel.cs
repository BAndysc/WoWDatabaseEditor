using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Windows;
using WDE.Common.Menu;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditor.Providers;
using WoWDatabaseEditorCore.Managers;

namespace WoWDatabaseEditorCore.ViewModels
{
    [SingleInstance]
    [AutoRegister]
    public class MainWindowViewModel : ObservableBase, ILayoutViewModelResolver, ICloseAwareViewModel
    {
        private readonly IMessageBoxService messageBoxService;
        private readonly Func<AboutViewModel> aboutViewModelCreator;
        private readonly Func<QuickStartViewModel> quickStartCreator;
        private readonly Func<TextDocumentViewModel> textDocumentCreator;

        private string title = "WoW Database Editor 2021.1";
        private readonly Dictionary<string, ITool> toolById = new();

        public MainWindowViewModel(IDocumentManager documentManager,
            IStatusBar statusBar,
            IMessageBoxService messageBoxService,
            TasksViewModel tasksViewModel,
            EditorMainMenuItemsProvider menuItemProvider,
            ISolutionSqlService solutionSqlService,
            Func<AboutViewModel> aboutViewModelCreator,
            Func<QuickStartViewModel> quickStartCreator,
            Func<TextDocumentViewModel> textDocumentCreator,
            ISolutionTasksService solutionTasksService,
            IMostRecentlyUsedService mostRecentlyUsedService,
            IEventAggregator eventAggregator)
        {
            DocumentManager = documentManager;
            StatusBar = statusBar;
            this.messageBoxService = messageBoxService;
            this.aboutViewModelCreator = aboutViewModelCreator;
            this.quickStartCreator = quickStartCreator;
            this.textDocumentCreator = textDocumentCreator;
            OpenDocument = new DelegateCommand<IMenuDocumentItem>(ShowDocument);
            ExecuteChangedCommand = new DelegateCommand(() =>
            {
                var item = DocumentManager.ActiveSolutionItemDocument?.SolutionItem;
                if (item == null)
                    return;

                if (DocumentManager.ActiveSolutionItemDocument!.Save?.CanExecute(null) ?? false)
                {
                    DocumentManager.ActiveSolutionItemDocument.Save.Execute(null);

                    if (solutionTasksService.CanReloadRemotely)
                        solutionTasksService.ReloadSolutionRemotelyTask(item);
                }
                else
                {
                    if (solutionTasksService.CanSaveAndReloadRemotely)
                        solutionTasksService.SaveAndReloadSolutionTask(item);
                    else if (solutionTasksService.CanSaveToDatabase)
                        solutionTasksService.SaveSolutionToDatabaseTask(item);
                }
            }, () => DocumentManager.ActiveSolutionItemDocument != null &&
                     (solutionTasksService.CanSaveAndReloadRemotely || solutionTasksService.CanSaveToDatabase));

            GenerateCurrentSqlCommand = new DelegateCommand(() =>
            {
                if (DocumentManager.ActiveDocument is ISolutionItemDocument {SolutionItem: { }} sid)
                    solutionSqlService.OpenDocumentWithSqlFor(sid.SolutionItem);
            }, () => DocumentManager.ActiveDocument != null && DocumentManager.ActiveDocument is ISolutionItemDocument);
            
            DocumentManager.ToObservable(dm => dm.ActiveDocument)
                .SubscribeAction(_ =>
                {
                    GenerateCurrentSqlCommand.RaiseCanExecuteChanged();
                    ExecuteChangedCommand.RaiseCanExecuteChanged();
                });
            
            TasksViewModel = tasksViewModel;

            MenuItemProviders = menuItemProvider.GetItems();

            foreach (var window in documentManager.AllTools)
                toolById[window.UniqueId] = window;

            if (GlobalApplication.Backend == GlobalApplication.AppBackend.Avalonia)
                ShowStartPage();
            else
                ShowAbout();
            //LoadDefault();
            
            Watch(DocumentManager, dm => dm.ActiveSolutionItemDocument, nameof(ShowExportButtons));
            Watch(DocumentManager, dm => dm.ActiveSolutionItemDocument, nameof(ShowPlayButtons));
            
            eventAggregator.GetEvent<AllModulesLoaded>()
                .Subscribe(OpenFatalLogIfExists, ThreadOption.PublisherThread, true);
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

        public IStatusBar StatusBar { get; }
        public IDocumentManager DocumentManager { get; }

        public TasksViewModel TasksViewModel { get; }

        public List<IMainMenuItem> MenuItemProviders { get; }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public DelegateCommand<IMenuDocumentItem> OpenDocument { get; }

        // this fallback to QuickStartViewModel is a hack, otherwise Avalonia for some reason will never show this button if it is not visible in the beginning
        public bool ShowPlayButtons => DocumentManager.ActiveSolutionItemDocument?.ShowExportToolbarButtons ?? (DocumentManager.ActiveDocument is QuickStartViewModel);
        
        public bool ShowExportButtons => DocumentManager.ActiveSolutionItemDocument?.ShowExportToolbarButtons ?? true;
        
        public DelegateCommand ExecuteChangedCommand { get; }
        
        public DelegateCommand GenerateCurrentSqlCommand { get; }
        
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

        public void LoadDefault()
        {
            foreach (var tool in toolById.Values)
            {
                if (tool.OpenOnStart)
                    DocumentManager.OpenTool(tool.GetType());
            }
        }

        public async Task<bool> CanClose()
        {
            var modifiedDocuments = DocumentManager.OpenedDocuments.Where(d => d.IsModified).ToList();

            if (modifiedDocuments.Count > 0)
            {
                while (modifiedDocuments.Count > 0)
                {
                    var editor = modifiedDocuments[^1];
                    var message = new MessageBoxFactory<MessageBoxButtonType>().SetTitle("Document is modified")
                        .SetMainInstruction("Do you want to save the changes of " + editor.Title + "?")
                        .SetContent("Your changes will be lost if you don't save them.")
                        .SetIcon(MessageBoxIcon.Warning)
                        .WithYesButton(MessageBoxButtonType.Yes)
                        .WithNoButton(MessageBoxButtonType.No)
                        .WithCancelButton(MessageBoxButtonType.Cancel);

                    if (modifiedDocuments.Count > 1)
                    {
                        message.SetExpandedInformation("Other modified documents:\n" +
                                                       string.Join("\n",
                                                           modifiedDocuments.SkipLast(1).Select(d => d.Title)));
                        message.WithButton("Yes to all", MessageBoxButtonType.CustomA)
                            .WithButton("No to all", MessageBoxButtonType.CustomB);
                    }

                    MessageBoxButtonType result = await messageBoxService.ShowDialog(message.Build());

                    if (result == MessageBoxButtonType.Cancel)
                        return false;

                    if (result == MessageBoxButtonType.Yes)
                    {
                        editor.Save.Execute(null);
                        modifiedDocuments.RemoveAt(modifiedDocuments.Count - 1);
                        DocumentManager.OpenedDocuments.Remove(editor);
                    }
                    else if (result == MessageBoxButtonType.No)
                    {
                        modifiedDocuments.RemoveAt(modifiedDocuments.Count - 1);
                        DocumentManager.OpenedDocuments.Remove(editor);
                    }
                    else if (result == MessageBoxButtonType.CustomA)
                    {
                        foreach (var m in modifiedDocuments)
                            m.Save.Execute(null);
                        modifiedDocuments.Clear();
                    }
                    else if (result == MessageBoxButtonType.CustomB)
                    {
                        modifiedDocuments.Clear();
                    }
                }
            }

            while (DocumentManager.OpenedDocuments.Count > 0)
                DocumentManager.OpenedDocuments.RemoveAt(DocumentManager.OpenedDocuments.Count - 1);

            return true;
        }

        public async Task<bool> TryClose()
        {
            if (!await CanClose())
                return false;
            
            CloseRequest?.Invoke();
            return true;
        }

        public void ForceClose()
        {
            ForceCloseRequest?.Invoke();
        }

        public event Action CloseRequest = delegate{};
        public event Action ForceCloseRequest = delegate{};
    }
}