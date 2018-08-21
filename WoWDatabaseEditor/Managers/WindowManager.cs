using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Windows;

namespace WoWDatabaseEditor.Managers
{
    public class WindowManager : BindableBase, IWindowManager
    {
        private readonly IUnityContainer _container;
        private readonly IEventAggregator _eventAggregator;

        public WindowManager(IUnityContainer container, IEventAggregator eventAggregator)
        {
            _container = container;
            _eventAggregator = eventAggregator;
        }
        
        private DocumentEditor _activeDocument;
        public DocumentEditor ActiveDocument
        {
            get => _activeDocument;
            set { SetProperty(ref _activeDocument, value); _eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value); }
        }

        public ObservableCollection<DocumentEditor> OpenedDocuments { get; } = new ObservableCollection<DocumentEditor>();
        public ObservableCollection<DocumentEditor> OpenedWindows { get; } = new ObservableCollection<DocumentEditor>();
        
        private HashSet<Type> OpenedWindowTypes { get; } = new HashSet<Type>();
        private  Dictionary<Type, DocumentEditor> Opened { get; } = new Dictionary<Type, DocumentEditor>();
        public void OpenDocument(DocumentEditor editor)
        {
            OpenedDocuments.Add(editor);
            ActiveDocument = editor;
        }

        public void OpenWindow(IWindowProvider provider)
        {
            if (!provider.AllowMultiple)
            {
                if (OpenedWindowTypes.Contains(provider.GetType()))
                {
                    Opened[provider.GetType()].Visibilityt = Visibility.Visible;
                    return;
                }

                OpenedWindowTypes.Add(provider.GetType());
            }

            var doc = new DocumentEditor
            {
                Title = provider.Name,
                Content = provider.GetView(),
                CloseCommand = new ActionCommand(a => { MessageBox.Show("aaaa"); }),
                CanClose = false
            };

            Opened.Add(provider.GetType(), doc);
            OpenedWindows.Add(doc);
        }
    }
}