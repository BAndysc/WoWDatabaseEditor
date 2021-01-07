using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Windows;
using Prism.Ioc;
using Prism.Commands;
using System.Windows.Controls;
using System.Windows.Input;
using WoWDatabaseEditor.Managers.ViewModels;

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
                if (value != null && documents.ContainsKey(value))
                    value = documents[value];
                SetProperty(ref _activeDocument, value);
                _eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value);
            }
        }

        public ObservableCollection<IDocument> OpenedDocuments { get; } = new ObservableCollection<IDocument>();
        public ObservableCollection<ITool> OpenedTools { get; } = new ObservableCollection<ITool>();         
        private Dictionary<Type, ToolWindow> Opened { get; } = new Dictionary<Type, ToolWindow>();

        private Dictionary<IDocument, DocumentDecorator> documents = new();
        
        public void OpenDocument(IDocument editor)
        {
            if (documents.ContainsKey(editor))
                ActiveDocument = documents[editor];
            else
            {
                var document = new DocumentDecorator(editor, doc =>
                {
                    documents.Remove(editor);
                    OpenedDocuments.Remove(doc);
                    if (ActiveDocument == doc)
                        ActiveDocument = null;
                    _eventAggregator.GetEvent<DocumentClosedEvent>().Publish(editor);
                });
                documents[editor] = document;
                OpenedDocuments.Add(document);
                ActiveDocument = document;   
            }
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

            var toolWindow = new ToolWindow(provider.Name, provider.GetView());
            
            if (!provider.AllowMultiple)
                Opened.Add(provider.GetType(), toolWindow);

            OpenedTools.Add(toolWindow);
        }

        public class DocumentClosedEvent : PubSubEvent<IDocument> {}
    }
}