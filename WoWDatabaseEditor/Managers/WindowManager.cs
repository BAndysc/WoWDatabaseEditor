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
        }
        
        private IDocument _activeDocument;
        public IDocument ActiveDocument
        {
            get => _activeDocument;
            set {
                if (value == null && OpenedDocuments.Contains(_activeDocument))
                    return;
                SetProperty(ref _activeDocument, value);
                _eventAggregator.GetEvent<EventActiveDocumentChanged>().Publish(value);
            }
        }

        public ObservableCollection<IDocument> OpenedDocuments { get; } = new ObservableCollection<IDocument>();
        public ObservableCollection<ITool> OpenedTools { get; } = new ObservableCollection<ITool>();         
        private Dictionary<Type, ToolWindow> Opened { get; } = new Dictionary<Type, ToolWindow>();

        public void OpenDocument(IDocument editor)
        {
            var document = new DocumentDecorator(editor, doc =>
            {
                OpenedDocuments.Remove(doc);
                if (ActiveDocument == doc)
                    ActiveDocument = null;
            });
            OpenedDocuments.Add(document);
            ActiveDocument = document;
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
    }
}