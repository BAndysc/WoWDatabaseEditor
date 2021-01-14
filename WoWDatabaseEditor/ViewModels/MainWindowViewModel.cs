using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Common.Windows;
using WoWDatabaseEditor.Managers;
using WoWDatabaseEditor.Utils;

namespace WoWDatabaseEditor.ViewModels
{
    public class MainWindowViewModel : BindableBase, ILayoutViewModelResolver
    {
        private readonly Dictionary<ISolutionItem, IDocument> documents = new();
        private readonly Dictionary<IDocument, ISolutionItem> documentToSolution = new();
        private readonly IEventAggregator eventAggregator;
        private readonly INewItemService newItemService;
        private readonly IConfigureService settings;
        private readonly ISolutionManager solutionManager;

        private string title = "Visual Database Editor 2018";
        private readonly Dictionary<string, ITool> toolById = new();

        public MainWindowViewModel(IEventAggregator eventAggregator,
            IWindowManager wndowManager,
            IConfigureService settings,
            INewItemService newItemService,
            ISolutionManager solutionManager,
            IStatusBar statusBar,
            ISolutionItemEditorRegistry solutionEditorManager,
            TasksViewModel tasksViewModel)
        {
            this.eventAggregator = eventAggregator;
            WindowManager = wndowManager;
            StatusBar = statusBar;
            this.settings = settings;
            this.newItemService = newItemService;
            this.solutionManager = solutionManager;
            ExecuteCommandNew = new DelegateCommand(New);
            ExecuteSettings = new DelegateCommand(SettingsShow);

            TasksViewModel = tasksViewModel;
            About = new DelegateCommand(ShowAbout);

            this.eventAggregator.GetEvent<WindowManager.DocumentClosedEvent>()
                .Subscribe(document =>
                {
                    if (!documentToSolution.ContainsKey(document))
                        return;

                    documents.Remove(documentToSolution[document]);
                    documentToSolution.Remove(document);
                });

            this.eventAggregator.GetEvent<EventRequestOpenItem>()
                .Subscribe(item =>
                    {
                        if (documents.ContainsKey(item))
                            WindowManager.OpenDocument(documents[item]);
                        else
                        {
                            IDocument? editor = solutionEditorManager.GetEditor(item);
                            if (editor == null)
                                MessageBox.Show("Editor for " + item.GetType() + " not registered.");
                            else
                            {
                                WindowManager.OpenDocument(editor);
                                documents[item] = editor;
                                documentToSolution[editor] = item;
                            }
                        }
                    },
                    true);

            Windows = new ObservableCollection<MenuItemViewModel>();

            foreach (var window in WindowManager.AllTools)
            {
                toolById[window.UniqueId] = window;
                MenuItemViewModel model = new(() => WindowManager.OpenTool(window.GetType()), window.Title);
                Windows.Add(model);
                //if (window.CanOpenOnStart)
                //    model.Command.Execute(null);
            }

            ShowAbout();
        }

        public IStatusBar StatusBar { get; }
        public IWindowManager WindowManager { get; }
        public TasksViewModel TasksViewModel { get; }
        

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public DelegateCommand ExecuteCommandNew { get; }
        public DelegateCommand ExecuteSettings { get; }
        public DelegateCommand About { get; }

        public ObservableCollection<MenuItemViewModel> Windows { get; set; }
        
        private void ShowAbout()
        {
            WindowManager.OpenDocument(new AboutViewModel());
        }

        private void SettingsShow()
        {
            settings.ShowSettings();
        }

        private void New()
        {
            ISolutionItem? item = newItemService.GetNewSolutionItem();
            if (item != null)
                solutionManager.Items.Add(item);
        }

        public ITool? ResolveViewModel(string id)
        {
            if (toolById.TryGetValue(id, out var tool))
            {
                WindowManager.OpenedTools.Add(tool);
                return tool;
            }
            return null;
        }

        public void LoadDefault()
        {
            foreach (var tool in toolById.Values)
            {
                if (tool.OpenOnStart)
                    WindowManager.OpenTool(tool.GetType());
            }
        }
    }
}