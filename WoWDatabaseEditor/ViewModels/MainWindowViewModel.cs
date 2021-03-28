using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Windows;
using WDE.Common.Menu;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WoWDatabaseEditor.Providers;

namespace WoWDatabaseEditorCore.ViewModels
{
    [SingleInstance]
    [AutoRegister]
    public class MainWindowViewModel : BindableBase, ILayoutViewModelResolver, ICloseAwareViewModel
    {
        private readonly IMessageBoxService messageBoxService;
        private readonly Func<AboutViewModel> aboutViewModelCreator;

        private string title = "Visual Database Editor 2018";
        private readonly Dictionary<string, ITool> toolById = new();

        public MainWindowViewModel(IDocumentManager documentManager,
            IStatusBar statusBar,
            IMessageBoxService messageBoxService,
            TasksViewModel tasksViewModel,
            EditorMainMenuItemsProvider menuItemProvider,
            ISolutionSqlService solutionSqlService,
            Func<AboutViewModel> aboutViewModelCreator)
        {
            DocumentManager = documentManager;
            StatusBar = statusBar;
            this.messageBoxService = messageBoxService;
            this.aboutViewModelCreator = aboutViewModelCreator;
            OpenDocument = new DelegateCommand<IMenuDocumentItem>(ShowDocument);
            ExecuteChangedCommand = new DelegateCommand(() =>
            {
                DocumentManager.ActiveDocument?.Save?.Execute(null);
            }, () => DocumentManager.ActiveDocument?.Save != null && DocumentManager.ActiveDocument.Save.CanExecute(null));

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

            ShowAbout();
            //LoadDefault();
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
        
        public DelegateCommand ExecuteChangedCommand { get; }
        
        public DelegateCommand GenerateCurrentSqlCommand { get; }
        
        private void ShowAbout()
        {
            DocumentManager.OpenDocument(aboutViewModelCreator());
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
                        .WithYesButton(MessageBoxButtonType.Ok)
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

            DocumentManager.OpenedDocuments.Clear();

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