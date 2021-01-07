using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Windows;
using WoWDatabaseEditor.Views;
using Prism.Ioc;
using WDE.Common.Solution;
using WoWDatabaseEditor.Managers;

namespace WoWDatabaseEditor.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigureService settings;
        private readonly INewItemService newItemService;
        private readonly ISolutionManager solutionManager;

        public IWindowManager WindowManager { get; private set; }

        private string _title = "Visual Database Editor 2018";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private readonly Dictionary<ISolutionItem, Document> documents = new ();
        private readonly Dictionary<IDocument, ISolutionItem> documentToSolution = new ();

        public DelegateCommand ExecuteCommandNew { get; private set; }
        public DelegateCommand ExecuteSettings { get; private set; }
        public DelegateCommand About { get; private set; }

        public ObservableCollection<MenuItemViewModel> Windows { get; set; }

        public MainWindowViewModel(IEventAggregator eventAggregator, IWindowManager wndowManager, IConfigureService settings, INewItemService newItemService, ISolutionManager solutionManager, Lazy<IEnumerable<IToolProvider>> tools, ISolutionItemEditorRegistry solutionEditorManager)
        {
            _eventAggregator = eventAggregator;
            WindowManager = wndowManager;
            this.settings = settings;
            this.newItemService = newItemService;
            this.solutionManager = solutionManager;
            ExecuteCommandNew = new DelegateCommand(New);
            ExecuteSettings = new DelegateCommand(SettingsShow);

            About = new DelegateCommand(ShowAbout);

            _eventAggregator.GetEvent<WindowManager.DocumentClosedEvent>().Subscribe(document =>
            {
                if (!documentToSolution.ContainsKey(document))
                    return;
                
                documents.Remove(documentToSolution[document]);
                documentToSolution.Remove(document);
            });
            
            _eventAggregator.GetEvent<EventRequestOpenItem>().Subscribe(item =>
            {
                if (documents.ContainsKey(item))
                    WindowManager.OpenDocument(documents[item]);
                else
                {
                    var editor = solutionEditorManager.GetEditor(item);
                    if (editor == null)
                        MessageBox.Show("Editor for " + item.GetType().ToString() + " not registered.");
                    else
                    {
                        WindowManager.OpenDocument(editor);
                        documents[item] = editor;
                        documentToSolution[editor] = item;
                    }
                }

            }, true);

            Windows = new ObservableCollection<MenuItemViewModel>();

            foreach (var window in tools.Value)
            {
                MenuItemViewModel model = new MenuItemViewModel(() => WindowManager.OpenTool(window), window.Name);
                Windows.Add(model);
                if (window.CanOpenOnStart)
                    model.Command.Execute(null);
            }
            ShowAbout();
        }

        private void ShowAbout()
        {
            Document about = new Document
            {
                Title = "About",
                Content = new AboutView(),
                CanClose = true,
//                CloseCommand = new DelegateCommand(() => { })
            };
            WindowManager.OpenDocument(about);
        }

        private void SettingsShow()
        {
            settings.ShowSettings();
        }

        private void New()
        {
            ISolutionItem item = newItemService.GetNewSolutionItem();
            if (item != null)
                solutionManager.Items.Add(item);
        }
    }
}
