using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Annotations;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.Solutions;
using WoWDatabaseEditorCore.Managers;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class EditorFileMenuItemProvider : IMainMenuItem, INotifyPropertyChanged
    {
        private readonly ISolutionManager solutionManager;
        private readonly IEventAggregator eventAggregator;
        private readonly INewItemService newItemService;
        private readonly IConfigureService settings;
        public IDocumentManager DocumentManager { get; }
        
        private readonly Dictionary<ISolutionItem, IDocument> documents = new();

        private readonly Dictionary<IDocument, ISolutionItem> documentToSolution = new();
        
        public string ItemName { get; } = "_File";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityVeryHigh;

        public EditorFileMenuItemProvider(ISolutionManager solutionManager, IEventAggregator eventAggregator, INewItemService newItemService, 
            ISolutionItemEditorRegistry solutionEditorManager, IMessageBoxService messageBoxService, IDocumentManager documentManager, IConfigureService settings,
            IApplication application, ISolutionTasksService solutionTasksService, ISolutionSqlService solutionSqlService)
        {
            this.solutionManager = solutionManager;
            this.eventAggregator = eventAggregator;
            this.newItemService = newItemService;
            DocumentManager = documentManager;
            this.settings = settings;
            SubItems = new List<IMenuItem>();
            if (solutionManager.CanContainAnyItem)
            {
                SubItems.Add(new ModuleMenuItem("_Add item to project", new AsyncAutoCommand(AddNewItemWindow), new("Control+N")));
                SubItems.Add(new ModuleMenuItem("_New / Open (without project)", new AsyncAutoCommand(OpenNewItemWindow), new("Control+O")));
            }
            else
            {
                SubItems.Add(new ModuleMenuItem("_New / Open", new AsyncAutoCommand(OpenNewItemWindow), new("Control+O")));
            }
            
            SubItems.Add(new ModuleMenuItem("_Save to database", 
                new DelegateCommand(
                        () =>
                        {
                            if (DocumentManager.ActiveSolutionItemDocument != null)
                                solutionTasksService.Save(DocumentManager.ActiveSolutionItemDocument!).ListenErrors();
                            else
                            {
                                async Task Func()
                                {
                                    if (documentManager.ActiveDocument is not IBeforeSaveConfirmDocument confirm || !await confirm.ShallSavePreventClosing()) 
                                        documentManager.ActiveDocument!.Save.Execute(null);
                                }

                                Func().ListenErrors();
                            }
                        },
                () => solutionTasksService.CanSaveToDatabase && (DocumentManager.ActiveDocument?.Save.CanExecute(null) ?? false))
                    .ObservesProperty(() => DocumentManager.ActiveDocument)
                    .ObservesProperty(() => DocumentManager.ActiveDocument!.IsModified), new("Control+S")));
            
            SubItems.Add(new ModuleMenuItem("_Generate query", 
                new DelegateCommand(
                        () => solutionSqlService.OpenDocumentWithSqlFor(DocumentManager.ActiveSolutionItemDocument!.SolutionItem),
                    () => DocumentManager.ActiveSolutionItemDocument != null)
                .ObservesProperty(() => DocumentManager.ActiveSolutionItemDocument), new("F4")));
            
            SubItems.Add(new ModuleMenuItem("_Save to database and reload server", new DelegateCommand(
                    () => solutionTasksService.SaveAndReloadSolutionTask(DocumentManager.ActiveSolutionItemDocument!.SolutionItem),
                    () => DocumentManager.ActiveSolutionItemDocument != null && solutionTasksService.CanSaveAndReloadRemotely)
                .ObservesProperty(() => DocumentManager.ActiveSolutionItemDocument), new("F5")));
            
            SubItems.Add(new ModuleMenuItem("_Generate query for all opened", 
                new DelegateCommand(
                        () => solutionSqlService.OpenDocumentWithSqlFor(DocumentManager.OpenedDocuments.Select(d => (d as ISolutionItemDocument)?.SolutionItem!).Where(d => d != null).ToArray<ISolutionItem>()))
                    , new("F3")));

            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new ModuleMenuItem("_Settings", new DelegateCommand(OpenSettings)));
            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new ModuleMenuItem("Close current tab", new AsyncAutoCommand(() => 
                documentManager.ActiveDocument?.CloseCommand?.ExecuteAsync() ?? Task.CompletedTask), new MenuShortcut("Control+W")));
            SubItems.Add(new ModuleMenuItem("_Exit", new DelegateCommand(() => application.TryClose())));
            
            this.eventAggregator.GetEvent<DocumentManager.DocumentClosedEvent>()
                .Subscribe(document =>
                {
                    if (!documentToSolution.ContainsKey(document))
                        return;

                    documents.Remove(documentToSolution[document]);
                    documentToSolution.Remove(document);
                });

            this.eventAggregator.GetEvent<EventRequestOpenItem>()
                .Subscribe(item =>
                    {
                        if (documents.ContainsKey(item))
                            DocumentManager.OpenDocument(documents[item]);
                        else
                        {
                            try
                            {
                                IDocument editor = solutionEditorManager.GetEditor(item);
                                DocumentManager.OpenDocument(editor);
                                documents[item] = editor;
                                documentToSolution[editor] = item;
                            }
                            catch (SolutionItemEditorNotFoundException)
                            {
                                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Editor not found")
                                    .SetMainInstruction(
                                        "Couldn't open item, because there is no editor registered for type " +
                                        item.GetType().Name)
#if DEBUG
                                    .SetContent(
                                        $"There should be class that implements ISolutionItemEditorProvider<{item.GetType().Name}> and this class should be registered in containerRegister in module.")
#endif
                                    .SetIcon(MessageBoxIcon.Warning)
                                    .WithOkButton(true)
                                    .Build());
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Cannot open the editor")
                                    .SetMainInstruction(
                                        "Couldn't open item, because there was an error")
                                    .SetContent(e.Message)
                                    .SetIcon(MessageBoxIcon.Error)
                                    .WithOkButton(true)
                                    .Build());
                            }
                        }
                    },
                    true);
        }

        private async Task AddNewItemWindow()
        {
            ISolutionItem? item = await newItemService.GetNewSolutionItem();
            if (item != null)
            {
                solutionManager.Add(item);
                if (item is not SolutionFolderItem)
                    eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
            }
        }
        
        private async Task OpenNewItemWindow()
        {
            ISolutionItem? item = await newItemService.GetNewSolutionItem(false);
            if (item != null)
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
        }
        private void OpenSettings() => settings.ShowSettings();

        public event PropertyChangedEventHandler? PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}