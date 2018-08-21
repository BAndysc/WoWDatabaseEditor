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
using WDE.Common.Windows;
using WoWDatabaseEditor.Events;
using WoWDatabaseEditor.Services.NewItemService;
using WoWDatabaseEditor.Views;

namespace WoWDatabaseEditor.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IUnityContainer _container;
        private readonly IEventAggregator _eventAggregator;

        public IWindowManager WindowManager { get; private set; }
        
        private string _title = "Visual Database Editor 2017";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private readonly Dictionary<ISolutionItem, DocumentEditor> documents = new Dictionary<ISolutionItem, DocumentEditor>();
        
        public DelegateCommand ExecuteCommandNew { get; private set; }
        public DelegateCommand ExecuteSettings { get; private set; }
        public DelegateCommand About { get; private set; }

        public ObservableCollection<MenuItemViewModel> Windows { get; set; }


        public MainWindowViewModel(IUnityContainer container, IEventAggregator eventAggregator, IWindowManager wndowManager)
        {
            _container = container;
            _eventAggregator = eventAggregator;
            WindowManager = wndowManager;
            
            ExecuteCommandNew = new DelegateCommand(New);
            ExecuteSettings = new DelegateCommand(SettingsShow);

            About = new DelegateCommand(ShowAbout);

            _eventAggregator.GetEvent<EventRequestOpenItem>().Subscribe(item =>
            {
                if (documents.ContainsKey(item))
                    WindowManager.ActiveDocument = documents[item];
                else
                {
                    var editor = container.Resolve<ISolutionEditorManager>().GetEditor(item);
                    if (editor == null)
                        MessageBox.Show("Editor for " + item.GetType().ToString() + " not registered.");
                    else
                    {
                        WindowManager.OpenDocument(editor);
                        documents[item] = editor;
                    }
                }

            }, true);

            Windows = new ObservableCollection<MenuItemViewModel>();

            _eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(() =>
            {
                var windows = container.ResolveAll<IWindowProvider>();
                foreach (var window in windows)
                {
                    Windows.Add(new MenuItemViewModel(() => WindowManager.OpenWindow(window)) { Header = window.Name });
                }
                ShowAbout();
            });
        }

        private void ShowAbout()
        {
            DocumentEditor about = new DocumentEditor
            {
                Title = "About",
                Content = new AboutView(),
                CanClose = true,
            };
            WindowManager.OpenDocument(about);
        }

        private void SettingsShow()
        {
            _container.Resolve<IConfigureService>().ShowSettings();
        }

        private void New()
        {
            ISolutionItem item = _container.Resolve<INewItemService>().GetNewSolutionItem();
            if (item != null)
                _container.Resolve<ISolutionManager>().Items.Add(item);
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
