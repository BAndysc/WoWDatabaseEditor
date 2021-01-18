using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class DocumentManager : BindableBase, IDocumentManager
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IMessageBoxService messageBoxService;
        private IDocument? activeDocument;
        private Dictionary<Type, ITool> typeToToolInstance = new();
        private List<ITool> allTools = new ();

        public DocumentManager(IEventAggregator eventAggregator, 
            IMessageBoxService messageBoxService,
            IEnumerable<ITool> tools)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            ActivateDocument = new DelegateCommand<IDocument>(doc => ActiveDocument = doc);
            foreach (var tool in tools)
            {
                allTools.Add(tool);
                typeToToolInstance[tool.GetType()] = tool;
            }
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
                if (value == null && activeDocument != null && OpenedDocuments.Contains(activeDocument))
                    return;
                SetProperty(ref activeDocument, value);
                eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value);
            }
        }


        public void OpenDocument(IDocument editor)
        {
            if (!OpenedDocuments.Contains(editor))
            {
                ICommand? origCommand = editor.CloseCommand;
                editor.CloseCommand = new Command(() =>
                    {
                        bool close = true;
                        if (editor.IsModified)
                        {
                            MessageBoxButtonType result = messageBoxService.ShowDialog(
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
                                editor.Save.Execute(null);
                        }

                        if (close)
                        {
                            origCommand?.Execute(null);
                            OpenedDocuments.Remove(editor);
                            eventAggregator.GetEvent<DocumentClosedEvent>().Publish(editor);   
                        }
                    },
                    () => origCommand?.CanExecute(null) ?? true);
                OpenedDocuments.Add(editor);
            }

            ActiveDocument = editor;
        }

        public void OpenTool<T>() where T : ITool
        {
            OpenTool(typeof(T));
        }

        public void OpenTool(Type toolType)
        {
            if (!typeToToolInstance.TryGetValue(toolType, out var tool))
                return;

            if (!OpenedTools.Contains(tool))
                OpenedTools.Add(tool);

            tool.Visibility = Visibility.Visible;
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