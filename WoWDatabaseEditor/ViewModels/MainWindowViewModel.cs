using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WoWDatabaseEditor.Services.NewItemService;
using WoWDatabaseEditor.Views;

namespace WoWDatabaseEditor.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        IUnityContainer container;
        private readonly IEventAggregator _eventAggregator;

        private string _title = "Visual Database Editor 2017";
        private ContentControl _activeDocument;

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private Dictionary<ISolutionItem, ContentControl> documents = new Dictionary<ISolutionItem, ContentControl>();

        public ContentControl SE => container.Resolve<ISolutionExplorer>().GetSolutionExplorerView();

        public ContentControl ActiveDocument
        {
            get { return _activeDocument; }
            set { if (value is DocumentEditor) SetProperty(ref _activeDocument, value); }
        }

        public DelegateCommand ExecuteCommandNew { get; private set; }
        public DelegateCommand ExecuteSettings { get; private set; }
        public DelegateCommand About { get; private set; }

        public ObservableCollection<DocumentEditor> Documents { get; private set; }

        public MainWindowViewModel(IUnityContainer container, IEventAggregator eventAggregator)
        {
            this.container = container;
            _eventAggregator = eventAggregator;
            ExecuteCommandNew = new DelegateCommand(New);
            ExecuteSettings = new DelegateCommand(SettingsShow);

            Documents = new ObservableCollection<DocumentEditor>();

            About = new DelegateCommand(ShowAbout);

            _eventAggregator.GetEvent<EventRequestOpenItem>().Subscribe(item =>
            {
                if (documents.ContainsKey(item))
                    ActiveDocument = documents[item];
                else
                {
                    var editor = container.Resolve<ISolutionEditorManager>().GetEditor(item);
                    if (editor == null)
                        MessageBox.Show("Editor for " + item.GetType().ToString() + " not registered.");
                    else
                    {
                        Documents.Add(editor);
                        documents[item] = editor;
                        ActiveDocument = editor;
                    }
                }

            }, true);
            
        }

        private void ShowAbout()
        {
            DocumentEditor about = new DocumentEditor
            {
                Title = "About",
                Content = new AboutView()
            };
            Documents.Add(about);
            ActiveDocument = about;
        }

        private void SettingsShow()
        {
            container.Resolve<IConfigureService>().ShowSettings();
        }

        private void New()
        {
            ISolutionItem item = container.Resolve<INewItemService>().GetNewSolutionItem();
            if (item != null)
                container.Resolve<ISolutionManager>().Items.Add(item);
        }

        public void DocumentClosed(object documentContent)
        {
            foreach (var key in documents.Keys)
            {
                if (documents[key] == documentContent)
                {
                    documents.Remove(key);
                    break;
                }
            }
        }
    }
}
