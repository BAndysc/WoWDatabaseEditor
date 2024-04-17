using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;
using WoWDatabaseEditorCore.Settings;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class DocumentManager : BindableBase, IDocumentManager
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IMessageBoxService messageBoxService;
        private readonly ISolutionTasksService solutionTasksService;
        private readonly ISolutionItemEditorRegistry solutionEditorManager;
        private readonly IGeneralEditorSettingsProvider generalEditorSettingsProvider;
        private ISolutionItemDocument? activeSolutionItemDocument;
        private IDocument? activeDocument;
        private IFocusableTool? activeTool;
        private ITool? selectedTool;
        private IUndoRedoWindow? activeUndoRedo;
        private Dictionary<Type, ITool> typeToToolInstance = new();
        private List<ITool> allTools = new ();

        private readonly Dictionary<ISolutionItem, IDocument> documents = new();

        private readonly Dictionary<IDocument, ISolutionItem> documentToSolution = new();

        public DocumentManager(IEventAggregator eventAggregator, 
            IMessageBoxService messageBoxService,
            ITextDocumentService textDocumentService,
            ISolutionTasksService solutionTasksService,
            ISolutionItemEditorRegistry solutionEditorManager,
            IGeneralEditorSettingsProvider generalEditorSettingsProvider,
            IEnumerable<ITool> tools)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.solutionTasksService = solutionTasksService;
            this.solutionEditorManager = solutionEditorManager;
            this.generalEditorSettingsProvider = generalEditorSettingsProvider;
            ActivateDocument = new DelegateCommand<IDocument>(doc => ActiveDocument = doc);
            foreach (var tool in tools)
            {
                allTools.Add(tool);
                tool.Visibility = false;
                typeToToolInstance[tool.GetType()] = tool;
            }
            allTools.Sort((a, b) => -a.OpenOnStart.CompareTo(b.OpenOnStart));

            OpenedDocuments.ToStream(false).SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Remove && e.Item is IDisposable disposable)
                    disposable.Dispose();
            });
            //OpenDocument(textDocumentService.CreateDocument("DEMO DEMO", "", "sql", true));
            
            
            this.eventAggregator.GetEvent<DocumentClosedEvent>()
                .Subscribe(document =>
                {

                });

            this.eventAggregator.GetEvent<EventRequestOpenItem>()
                .Subscribe(item =>
                    {
                        OpenDocument(item);
                    },
                    true);
        }

        private void RemoveDocument(IDocument document)
        {
            OpenedDocuments.Remove(document);

            if (!documentToSolution.ContainsKey(document))
                return;

            documents.Remove(documentToSolution[document]);
            documentToSolution.Remove(document);
        }

        public IReadOnlyList<ITool> AllTools => allTools;
        public ObservableCollection<IDocument> OpenedDocuments { get; } = new();
        public ObservableCollection<ITool> OpenedTools { get; } = new();
        public DelegateCommand<IDocument> ActivateDocument { get; }

        public IDocument? ActiveDocument
        {
            get => activeDocument;
            set
            {
                if (value == null && activeDocument != null && OpenedDocuments.Contains(activeDocument) && activeTool == null)
                    return;
                if (value != null)
                {
                    ActiveTool = null;
                    if (value is IUndoRedoWindow undoRedoWindow)
                        ActiveUndoRedo = undoRedoWindow;
                } else if (ActiveUndoRedo == activeDocument && activeDocument != null)
                    ActiveUndoRedo = null;
                activeSolutionItemDocument = value as ISolutionItemDocument;
                if (activeDocument != value)
                {
                    SetProperty(ref activeDocument, value);
                    RaisePropertyChanged(nameof(ActiveSolutionItemDocument));
                }
                SelectedTool = null;
                eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value);
            }
        }
        
        public async Task<bool> TryCloseAllDocuments(bool closingEditor)
        {
            var modifiedDocuments = OpenedDocuments.Where(d => d.IsModified).ToList();
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
                        if (editor is IBeforeSaveConfirmDocument before)
                        {
                            if (await before.ShallSavePreventClosing())
                                return false;
                        }
                        //editor.Save.Execute(null);
                        if (editor is ISolutionItemDocument solutionItemDocument)
                            await solutionTasksService.Save(solutionItemDocument);
                        else
                        {
                            if (editor.Save is IAsyncCommand async)
                                await async.ExecuteAsync();
                            else
                                editor.Save.Execute(null);
                        }
                        modifiedDocuments.RemoveAt(modifiedDocuments.Count - 1);
                        RemoveDocument(editor);
                    }
                    else if (result == MessageBoxButtonType.No)
                    {
                        modifiedDocuments.RemoveAt(modifiedDocuments.Count - 1);
                        RemoveDocument(editor);
                    }
                    else if (result == MessageBoxButtonType.CustomA)
                    {
                        foreach (var m in modifiedDocuments)
                        {
                            if (m is IBeforeSaveConfirmDocument before)
                            {
                                if (await before.ShallSavePreventClosing())
                                {
                                    return false;
                                }
                            }
                            if (m is ISolutionItemDocument solutionItemDocument)
                                await solutionTasksService.Save(solutionItemDocument);
                            else
                                m.Save.Execute(null);
                        }
                        modifiedDocuments.Clear();
                    }
                    else if (result == MessageBoxButtonType.CustomB)
                    {
                        modifiedDocuments.Clear();
                    }
                }
            }

            // when always restore is enabled, don't close documents so that the session can be saved
            if (!closingEditor || generalEditorSettingsProvider.RestoreOpenTabsMode != RestoreOpenTabsMode.AlwaysRestore)
            {
                while (OpenedDocuments.Count > 0)
                    RemoveDocument(OpenedDocuments[OpenedDocuments.Count - 1]);
            }
            
            var modifiedTools = AllTools
                .Select(t => t as ISavableTool)
                .Where(t => t != null)
                .Cast<ISavableTool>()
                .Where(t => t.IsModified)
                .ToList();

            foreach (var tool in modifiedTools)
            {
                var message = new MessageBoxFactory<MessageBoxButtonType>().SetTitle("Tool is modified")
                    .SetMainInstruction("Do you want to save the changes of " + tool.Title + "?")
                    .SetContent("Your changes will be lost if you don't save them.")
                    .SetIcon(MessageBoxIcon.Warning)
                    .WithYesButton(MessageBoxButtonType.Yes)
                    .WithNoButton(MessageBoxButtonType.No)
                    .WithCancelButton(MessageBoxButtonType.Cancel);

                MessageBoxButtonType result = await messageBoxService.ShowDialog(message.Build());
                if (result == MessageBoxButtonType.Cancel)
                    return false;

                if (result == MessageBoxButtonType.Yes)
                {
                    await SaveWithTimeout(tool);
                }
                else if (result == MessageBoxButtonType.No)
                {
                }
            }
            
            return true;    
        }

        private async Task SaveWithTimeout(ISavableTool tool)
        {
            var delay = Task.Delay(10000);

            var finishedTask = await Task.WhenAny(delay, tool.Save.ExecuteAsync());

            if (finishedTask == delay)
            {
                if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Error while saving")
                        .SetMainInstruction("Couldn't save " + tool.Title)
                        .SetContent(
                            "The save operation timed out. It might be fatal error or just connection problems. Do you want to try again?")
                        .WithYesButton(true)
                        .WithNoButton(false)
                        .Build()))
                    await SaveWithTimeout(tool);
            }
        }

        public bool BackgroundMode { get; private set; }

        public void ActivateDocumentInTheBackground(IDocument document)
        {
            BackgroundMode = true;
            ActiveDocument = document;
            BackgroundMode = false;
        }

        public IFocusableTool? ActiveTool
        {
            get => activeTool;
            set
            {
                var oldActiveTool = activeTool;
                activeTool = value;
                if (value != null)
                {
                    ActiveDocument = null;
                    if (value is IUndoRedoWindow undoRedoWindow)
                        ActiveUndoRedo = undoRedoWindow;
                } else if (ActiveUndoRedo == oldActiveTool && oldActiveTool != null)
                    ActiveUndoRedo = null;
                RaisePropertyChanged(nameof(ActiveTool));
            }
        }

        public ITool? SelectedTool
        {
            get => selectedTool;
            set
            {
                if (selectedTool == value)
                    return;
                if (selectedTool != null)
                    selectedTool.IsSelected = false;
                selectedTool = value;
                if (value != null)
                    value.IsSelected = true;
                RaisePropertyChanged(nameof(SelectedTool));
            }
        }

        public IUndoRedoWindow? ActiveUndoRedo
        {
            get => activeUndoRedo;
            private set
            {
                SetProperty(ref activeUndoRedo, value);
                eventAggregator.GetEvent<EventActiveUndoRedoDocumentChanged>().Publish(value);
            }
        }

        public ISolutionItemDocument? ActiveSolutionItemDocument => activeSolutionItemDocument;

        public void OpenDocument(IDocument editor)
        {
            if (!OpenedDocuments.Contains(editor))
            {
                IAsyncCommand? origCommand = editor.CloseCommand;
                editor.CloseCommand = new AsyncCommand(async () =>
                {
                    bool close = true;
                    if (editor.IsModified)
                    {
                        MessageBoxButtonType result = await messageBoxService.ShowDialog(
                            new MessageBoxFactory<MessageBoxButtonType>().SetTitle("Document is modified")
                                .SetMainInstruction("Do you want to save the changes of " + editor.Title + "?")
                                .SetContent("Your changes will be lost if you don't save them.")
                                .SetIcon(MessageBoxIcon.Warning)
                                .WithYesButton(MessageBoxButtonType.Yes)
                                .WithNoButton(MessageBoxButtonType.No)
                                .WithCancelButton(MessageBoxButtonType.Cancel)
                                .Build());

                        if (result == MessageBoxButtonType.Cancel)
                            close = false;
                        if (result == MessageBoxButtonType.Yes)
                        {
                            if (editor is IBeforeSaveConfirmDocument preventable)
                            {
                                if (await preventable.ShallSavePreventClosing())
                                {
                                    close = false;
                                }
                            }

                            if (close)
                            {
                                if (editor is ISolutionItemDocument solutionItemDocument)
                                    await solutionTasksService.Save(solutionItemDocument);
                                else
                                    editor.Save.Execute(null);
                            }
                        }
                    }

                    if (close)
                    {
                        if (origCommand != null)
                            await origCommand.ExecuteAsync();
                        RemoveDocument(editor);
                        editor.CloseCommand = origCommand;
                    }
                },
                _ => origCommand?.CanExecute(null) ?? true);
                OpenedDocuments.Add(editor);
            }

            ActiveDocument = editor;
        }

        public IDocument? OpenDocument(ISolutionItem item)
        {
            if (documents.ContainsKey(item))
            {
                OpenDocument(documents[item]);
                return documents[item];
            }
            else
            {
                try
                {
                    IDocument editor = solutionEditorManager.GetEditor(item);
                    OpenDocument(editor);
                    documents[item] = editor;
                    documentToSolution[editor] = item;
                    return editor;
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
                    return null;
                }
                catch (Exception e)
                {
                    LOG.LogError(e);
                    messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Cannot open the editor")
                        .SetMainInstruction(
                            "Couldn't open item, because there was an error")
                        .SetContent(e.Message)
                        .SetIcon(MessageBoxIcon.Error)
                        .WithOkButton(true)
                        .Build());
                    return null;
                }
            }
        }

        public void OpenTool<T>() where T : ITool
        {
            OpenTool(typeof(T));
        }

        public T GetTool<T>() where T : ITool
        {
            if (!typeToToolInstance.TryGetValue(typeof(T), out var tool))
                throw new Exception("tool doesnt exist");
            return (T)tool;
        }
        
        public void OpenTool(Type toolType)
        {
            if (!typeToToolInstance.TryGetValue(toolType, out var tool))
                return;

            if (!OpenedTools.Contains(tool))
                OpenedTools.Add(tool);

            tool.Visibility = true;
            tool.IsSelected = true;
        }

        private class Command : ICommand
        {
            private readonly Action action;
            private readonly Func<bool> canExecute;

            public Command(Action action, Func<bool> canExecute)
            {
                this.action = action;
                this.canExecute = canExecute;
            }

            public bool CanExecute(object? parameter)
            {
                return canExecute.Invoke();
            }

            public void Execute(object? parameter)
            {
                action();
            }

            public event EventHandler? CanExecuteChanged;
        }

        public class DocumentClosedEvent : PubSubEvent<IDocument>
        {
        }
    }
}