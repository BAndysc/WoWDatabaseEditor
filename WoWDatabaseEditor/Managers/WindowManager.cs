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
        
        private Document _activeDocument;
        public Document ActiveDocument
        {
            get => _activeDocument;
            set { SetProperty(ref _activeDocument, value); _eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value); }
        }

        public ObservableCollection<Document> OpenedDocuments { get; } = new ObservableCollection<Document>();
        public ObservableCollection<Document> OpenedTools { get; } = new ObservableCollection<Document>();
        
        private HashSet<Type> OpenedToolTypes { get; } = new HashSet<Type>();
        private  Dictionary<Type, Document> Opened { get; } = new Dictionary<Type, Document>();
        public void OpenDocument(Document editor)
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
                    //todo
                    //Opened[provider.GetType()].Visibilityt = Visibility.Visible;
                    return;
                }

                OpenedToolTypes.Add(provider.GetType());
            }

            var doc = new Document
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