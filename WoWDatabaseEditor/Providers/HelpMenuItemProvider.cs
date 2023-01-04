using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Managers;
using WoWDatabaseEditorCore.Services.LogService.ViewModels;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class HelpMenuItemProvider : IMainMenuItem, INotifyPropertyChanged
    {
        private readonly Func<AboutViewModel> aboutViewModelCreator;
        private readonly Func<LogViewerControlViewModel> debugConsole;
        private readonly ITextDocumentService textDocumentService;
        public IDocumentManager DocumentManager { get; }
        
        public string ItemName { get; } = "_Help";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityLow;

        public HelpMenuItemProvider(IDocumentManager documentManager, IConfigureService settings,
            Func<AboutViewModel> aboutViewModelCreator, 
            Func<LogViewerControlViewModel> debugConsole,
            ITextDocumentService textDocumentService,
            IReportBugService reportBugService)
        {
            DocumentManager = documentManager;
            this.aboutViewModelCreator = aboutViewModelCreator;
            this.debugConsole = debugConsole;
            this.textDocumentService = textDocumentService;
            SubItems = new List<IMenuItem>();
            SubItems.Add(new ModuleMenuItem("Report a bug", new DelegateCommand(reportBugService.ReportBug)));
            SubItems.Add(new ModuleMenuItem("Send feedback", new DelegateCommand(reportBugService.SendFeedback)));
            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new ModuleMenuItem("Open debug console", new DelegateCommand(OpenDebugConsole)));
            SubItems.Add(new ModuleMenuItem("Debug clear unused memory", new DelegateCommand(CallGC)));
            SubItems.Add(new ModuleMenuItem("Open debug SQL console", new DelegateCommand(OpenDebugSqlConsole)));
            SubItems.Add(new ModuleManuSeparatorItem());
            SubItems.Add(new ModuleMenuItem("About", new DelegateCommand(OpenAbout)));
        }

        private void OpenDebugSqlConsole()
        {
            DocumentManager.OpenDocument(textDocumentService.CreateDocument( $"Debug SQL console", "", "sql", true));
        }

        private void CallGC()
        {
            for (int i = 0; i < 100; ++i)
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            }
        }

        private void OpenAbout() => DocumentManager.OpenDocument(aboutViewModelCreator());

        private void OpenDebugConsole() => DocumentManager.OpenDocument(debugConsole());

        public event PropertyChangedEventHandler? PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}