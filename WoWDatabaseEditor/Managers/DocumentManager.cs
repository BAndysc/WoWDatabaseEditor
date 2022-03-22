using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class DocumentManager : BindableBase, IDocumentManager
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IMessageBoxService messageBoxService;
        private readonly ISolutionTasksService solutionTasksService;
        private ISolutionItemDocument? activeSolutionItemDocument;
        private IDocument? activeDocument;
        private IFocusableTool? activeTool;
        private ITool? selectedTool;
        private IUndoRedoWindow? activeUndoRedo;
        private Dictionary<Type, ITool> typeToToolInstance = new();
        private List<ITool> allTools = new ();

        public DocumentManager(IEventAggregator eventAggregator, 
            IMessageBoxService messageBoxService,
            ITextDocumentService textDocumentService,
            ISolutionTasksService solutionTasksService,
            IEnumerable<ITool> tools)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.solutionTasksService = solutionTasksService;
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
                SetProperty(ref activeDocument, value);
                RaisePropertyChanged(nameof(ActiveSolutionItemDocument));
                SelectedTool = null;
                eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value);
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
                                    solutionTasksService.Save(solutionItemDocument).ListenErrors();
                                else
                                    editor.Save.Execute(null);
                            }
                        }
                    }

                    if (close)
                    {
                        if (origCommand != null)
                            await origCommand.ExecuteAsync();
                        OpenedDocuments.Remove(editor);
                        if (ActiveDocument == editor)
                            ActiveDocument = OpenedDocuments.Count > 0 ? OpenedDocuments[0] : null;
                        eventAggregator.GetEvent<DocumentClosedEvent>().Publish(editor);
                        editor.CloseCommand = origCommand;
                    }
                },
                _ => origCommand?.CanExecute(null) ?? true);
                OpenedDocuments.Add(editor);
            }

            ActiveDocument = editor;
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