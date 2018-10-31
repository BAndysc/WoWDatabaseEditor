using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Expression.Interactivity.Core;

using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Windows;
using Prism.Ioc;

namespace WoWDatabaseEditor.Managers
{
    [WDE.Module.Attributes.AutoRegister, WDE.Module.Attributes.SingleInstance]
    public class WindowManager : BindableBase, IWindowManager
    {
        private readonly IEventAggregator _eventAggregator;

        public WindowManager(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        
        private DocumentEditor _activeDocument;
        public DocumentEditor ActiveDocument
        {
            get => _activeDocument;
            set { SetProperty(ref _activeDocument, value); _eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value); }
        }

        public ObservableCollection<DocumentEditor> OpenedDocuments { get; } = new ObservableCollection<DocumentEditor>();
        public ObservableCollection<DocumentEditor> OpenedTools { get; } = new ObservableCollection<DocumentEditor>();
        
        private HashSet<Type> OpenedToolTypes { get; } = new HashSet<Type>();
        private  Dictionary<Type, DocumentEditor> Opened { get; } = new Dictionary<Type, DocumentEditor>();
        public void OpenDocument(DocumentEditor editor)
        {
            OpenedDocuments.Add(editor);
            ActiveDocument = editor;
        }

        public void OpenTool(IToolProvider provider)
        {
            if (!provider.AllowMultiple)
            {
                if (OpenedToolTypes.Contains(provider.GetType()))
                {
                    Opened[provider.GetType()].Visibilityt = Visibility.Visible;
                    return;
                }

                OpenedToolTypes.Add(provider.GetType());
            }

            var doc = new DocumentEditor
            {
                Title = provider.Name,
                Content = provider.GetView(),
                CloseCommand = new ActionCommand(a => { MessageBox.Show("aaaa"); }),
                CanClose = true
            };

            Opened.Add(provider.GetType(), doc);
            OpenedTools.Add(doc);
        }
    }
}