using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
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
            ITablesToolService tablesToolService)
        {
            DocumentManager = documentManager;
            SubItems = new List<IMenuItem>();
            var undo = new DelegateCommand(() =>
                    {
                        DocumentManager.ActiveUndoRedo?.Undo.Execute(null);
                    },
                    () => DocumentManager.ActiveUndoRedo?.Undo.CanExecute(null) ?? false)
                .ObservesProperty(() => DocumentManager.ActiveUndoRedo)
                .ObservesProperty(() => DocumentManager.ActiveUndoRedo!.IsModified);
            SubItems.Add(new ModuleMenuItem("_Undo", undo, new("Control+Z")));

            var redo = new DelegateCommand(() => DocumentManager.ActiveUndoRedo?.Redo.Execute(null),
                    () => DocumentManager.ActiveUndoRedo?.Redo.CanExecute(null) ?? false)
                .ObservesProperty(() => DocumentManager.ActiveUndoRedo)
                .ObservesProperty(() => DocumentManager.ActiveUndoRedo!.IsModified);
            SubItems.Add(new ModuleMenuItem("_Redo", redo, new("Control+Y")));
            
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
            
            
            DocumentManager
                .ToObservable(x => x.ActiveUndoRedo)
                .Select(x => x?.Undo ?? AlwaysDisabledCommand.Command)
                .Select(x => x.ToObservable())
                .Switch()
                .Subscribe(_ => undo.RaiseCanExecuteChanged());
            DocumentManager
                .ToObservable(x => x.ActiveUndoRedo)
                .Select(x => x?.Redo ?? AlwaysDisabledCommand.Command)
                .Select(x => x.ToObservable())
                .Switch()
                .Subscribe(_ => redo.RaiseCanExecuteChanged());

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}