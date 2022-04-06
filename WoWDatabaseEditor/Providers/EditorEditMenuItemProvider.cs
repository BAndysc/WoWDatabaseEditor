using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.FindAnywhere;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class EditorEditMenuItemProvider : IMainMenuItem, INotifyPropertyChanged
    {
        public string ItemName => "_Edit";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority => MainMenuItemSortPriority.PriorityHigh;
        public IDocumentManager DocumentManager { get; }

        public EditorEditMenuItemProvider(IDocumentManager documentManager,
            ITablesToolService tablesToolService,
            IQuickAccessViewModel quickAccessViewModel,
            Lazy<IWindowManager> windowManager,
            Func<IFindAnywhereDialogViewModel> findAnywhereDialog)
        {
            DocumentManager = documentManager;
            SubItems = new List<IMenuItem>();
            SubItems.Add(new ModuleMenuItem("_Undo", new DelegateCommand(() =>
                {
                    DocumentManager.ActiveUndoRedo?.Undo.Execute(null);
                },
                () => DocumentManager.ActiveUndoRedo?.Undo.CanExecute(null) ?? false)
                .ObservesProperty(() => DocumentManager.ActiveUndoRedo)
                .ObservesProperty(() => DocumentManager.ActiveUndoRedo!.IsModified), new("Control+Z")));
            
            SubItems.Add(new ModuleMenuItem("_Redo", new DelegateCommand(() => DocumentManager.ActiveUndoRedo?.Redo.Execute(null),
                () => DocumentManager.ActiveUndoRedo?.Redo.CanExecute(null) ?? false).ObservesProperty(() => DocumentManager.ActiveUndoRedo).
                ObservesProperty(() => DocumentManager.ActiveUndoRedo!.IsModified), new("Control+Y")));
            
            SubItems.Add(new ModuleManuSeparatorItem());
            
            SubItems.Add(new ModuleMenuItem("_Copy", 
                new DelegateCommand(
                    () => DocumentManager.ActiveDocument?.Copy.Execute(null),
                () => DocumentManager.ActiveDocument?.Copy.CanExecute(null) ?? false)
                    .ObservesProperty(() => DocumentManager.ActiveDocument), new("Control+C")));
            
            SubItems.Add(new ModuleMenuItem("Cu_t", 
                new DelegateCommand(
                    () => DocumentManager.ActiveDocument?.Cut.Execute(null),
                () => DocumentManager.ActiveDocument?.Cut.CanExecute(null) ?? false)
                    .ObservesProperty(() => DocumentManager.ActiveDocument), new("Control+X")));
            
            SubItems.Add(new ModuleMenuItem("_Paste", 
                new DelegateCommand(() => DocumentManager.ActiveDocument?.Paste.Execute(null),
                () => DocumentManager.ActiveDocument?.Paste.CanExecute(null) ?? false)
                    .ObservesProperty(() => DocumentManager.ActiveDocument), new("Control+V")));
            
            SubItems.Add(new ModuleManuSeparatorItem());
            
            SubItems.Add(new ModuleMenuItem("Toggle tables view",
                new DelegateCommand(() =>
                {
                    if (tablesToolService.Visibility)
                        tablesToolService.Close();
                    else
                        tablesToolService.Open();
                }),
                new("Control+T")));

            SubItems.Add(new ModuleMenuItem("Open quick access",
                new DelegateCommand(() => quickAccessViewModel.OpenSearch("")),
                new("Control+R")));
            
            SubItems.Add(new ModuleMenuItem("Open quick commands",
                new DelegateCommand(() => quickAccessViewModel.OpenSearch("/")),
                new("Control+Shift+R")));
            
            SubItems.Add(new ModuleManuSeparatorItem());
            
            SubItems.Add(new ModuleMenuItem("Find anywhere",
                new AsyncAutoCommand(() => windowManager.Value.ShowDialog(findAnywhereDialog())),
                new("Control+Shift+F")));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}