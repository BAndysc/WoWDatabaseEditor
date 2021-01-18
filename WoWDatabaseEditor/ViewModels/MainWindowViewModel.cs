using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Windows;
using WoWDatabaseEditor.Managers;
using WoWDatabaseEditor.Utils;

namespace WoWDatabaseEditor.ViewModels
{
    public class MainWindowViewModel : BindableBase, ILayoutViewModelResolver, ICloseAwareViewModel
    {
        private readonly Dictionary<ISolutionItem, IDocument> documents = new();
        private readonly Dictionary<IDocument, ISolutionItem> documentToSolution = new();
        private readonly IEventAggregator eventAggregator;
        private readonly INewItemService newItemService;
        private readonly IConfigureService settings;
        private readonly ISolutionManager solutionManager;
        private readonly IMessageBoxService messageBoxService;

        private string title = "Visual Database Editor 2018";
        private readonly Dictionary<string, ITool> toolById = new();

        public MainWindowViewModel(IEventAggregator eventAggregator,
            IDocumentManager documentManager,
            IConfigureService settings,
            INewItemService newItemService,
            ISolutionManager solutionManager,
            IStatusBar statusBar,
            IMessageBoxService messageBoxService,
            ISolutionItemEditorRegistry solutionEditorManager,
            TasksViewModel tasksViewModel)
        {
            this.eventAggregator = eventAggregator;
            DocumentManager = documentManager;
            StatusBar = statusBar;
            this.settings = settings;
            this.newItemService = newItemService;
            this.solutionManager = solutionManager;
            this.messageBoxService = messageBoxService;
            ExecuteCommandNew = new DelegateCommand(New);
            ExecuteSettings = new DelegateCommand(SettingsShow);

            TasksViewModel = tasksViewModel;
            About = new DelegateCommand(ShowAbout);

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
                            catch (SolutionItemEditorNotFoundException e)
                            {
                                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Editor not found")
                                    .SetMainInstruction("Couldn't open item, because there is no editor registered for type " +
                                                        item.GetType().Name)
                                    #if DEBUG
                                    .SetContent($"There should be class that implements ISolutionItemEditorProvider<{item.GetType().Name}> and this class should be registered in containerRegister in module.")
                                    #endif
                                    .SetIcon(MessageBoxIcon.Warning)
                                    .WithOkButton(true)
                                    .Build());
                            }
                        }
                    },
                    true);

            Windows = new ObservableCollection<MenuItemViewModel>();

            foreach (var window in DocumentManager.AllTools)
            {
                toolById[window.UniqueId] = window;
                MenuItemViewModel model = new(() => DocumentManager.OpenTool(window.GetType()), window.Title);
                Windows.Add(model);
                //if (window.CanOpenOnStart)
                //    model.Command.Execute(null);
            }

            ShowAbout();
        }

        public IStatusBar StatusBar { get; }
        public IDocumentManager DocumentManager { get; }
        public TasksViewModel TasksViewModel { get; }
        

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public DelegateCommand ExecuteCommandNew { get; }
        public DelegateCommand ExecuteSettings { get; }
        public DelegateCommand About { get; }

        public ObservableCollection<MenuItemViewModel> Windows { get; set; }
        
        private void ShowAbout()
        {
            DocumentManager.OpenDocument(new AboutViewModel());
        }

        private void SettingsShow()
        {
            settings.ShowSettings();
        }

        private void New()
        {
            ISolutionItem? item = newItemService.GetNewSolutionItem();
            if (item != null)
            {
                solutionManager.Items.Add(item);
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
            }
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

        public bool CanClose()
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
                                                       string.Join("\n", modifiedDocuments.SkipLast(1).Select(d => d.Title)));
                        message.WithButton("Yes to all", MessageBoxButtonType.CustomA)
                            .WithButton("No to all", MessageBoxButtonType.CustomB);
                    }
                    
                    MessageBoxButtonType result = messageBoxService.ShowDialog(message.Build());

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
    }
}