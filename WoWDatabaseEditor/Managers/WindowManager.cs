using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Windows;
using Prism.Commands;
using System.Windows.Input;

namespace WoWDatabaseEditor.Managers
{
    [WDE.Module.Attributes.AutoRegister, WDE.Module.Attributes.SingleInstance]
    public class WindowManager : BindableBase, IWindowManager
    {
        private readonly IEventAggregator _eventAggregator;

        public WindowManager(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            ActivateDocument = new DelegateCommand<IDocument>(doc => ActiveDocument = doc);
        }
        
        public DelegateCommand<IDocument> ActivateDocument { get; }
        private IDocument? _activeDocument;
        public IDocument? ActiveDocument
        {
            get => _activeDocument;
            set {
                if (value == null && _activeDocument != null && OpenedDocuments.Contains(_activeDocument))
                    return;
                SetProperty(ref _activeDocument, value);
                _eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value);
            }
        }

        public ObservableCollection<IDocument> OpenedDocuments { get; } = new ObservableCollection<IDocument>();
        public ObservableCollection<ITool> OpenedTools { get; } = new ObservableCollection<ITool>();         
        private Dictionary<Type, ITool> Opened { get; } = new Dictionary<Type, ITool>();

        public void OpenDocument(IDocument editor)
        {
            if (!OpenedDocuments.Contains(editor))
            {
                var origCommand = editor.CloseCommand;
                editor.CloseCommand = new Command(() =>
                {
                    origCommand?.Execute(null);
                    OpenedDocuments.Remove(editor);
                    _eventAggregator.GetEvent<DocumentClosedEvent>().Publish(editor);
                }, () => origCommand?.CanExecute(null) ?? true);
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

            var tool = provider.Provide();
            
            if (!provider.AllowMultiple)
                Opened.Add(provider.GetType(), tool);

            OpenedTools.Add(tool);
        }

        private class Command : ICommand
        {
            public readonly Func<bool> canExecute;
            public readonly Action action;

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
        
        public class DocumentClosedEvent : PubSubEvent<IDocument> {}
    }
}