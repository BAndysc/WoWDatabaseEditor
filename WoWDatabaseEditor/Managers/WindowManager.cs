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
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class WindowManager : BindableBase, IWindowManager
    {
        private readonly IEventAggregator eventAggregator;
        private IDocument? activeDocument;

        public WindowManager(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
            ActivateDocument = new DelegateCommand<IDocument>(doc => ActiveDocument = doc);
        }

        public DelegateCommand<IDocument> ActivateDocument { get; }
        private Dictionary<Type, ITool> Opened { get; } = new();

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

        public ObservableCollection<IDocument> OpenedDocuments { get; } = new();
        public ObservableCollection<ITool> OpenedTools { get; } = new();

        public void OpenDocument(IDocument editor)
        {
            if (!OpenedDocuments.Contains(editor))
            {
                ICommand? origCommand = editor.CloseCommand;
                editor.CloseCommand = new Command(() =>
                    {
                        origCommand?.Execute(null);
                        OpenedDocuments.Remove(editor);
                        eventAggregator.GetEvent<DocumentClosedEvent>().Publish(editor);
                    },
                    () => origCommand?.CanExecute(null) ?? true);
                OpenedDocuments.Add(editor);
            }

            ActiveDocument = editor;
        }

        public void OpenTool(IToolProvider provider)
        {
            if (!provider.AllowMultiple)
            {
                if (Opened.ContainsKey(provider.GetType()))
                {
                    Opened[provider.GetType()].Visibility = Visibility.Visible;
                    return;
                }
            }

            ITool? tool = provider.Provide();

            if (!provider.AllowMultiple)
                Opened.Add(provider.GetType(), tool);

            OpenedTools.Add(tool);
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