using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class EditorEditMenuItemProvider: IMainMenuItem, INotifyPropertyChanged
    {
        public string ItemName { get; } = "_Edit";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityHigh;
        public IDocumentManager DocumentManager { get; }

        public EditorEditMenuItemProvider(IDocumentManager documentManager)
        {
            DocumentManager = documentManager;
            SubItems = new List<IMenuItem>();
            SubItems.Add(new ModuleMenuItem("_Undo", new DelegateCommand(() => DocumentManager.ActiveDocument?.Undo.Execute(null),
                () => DocumentManager.ActiveDocument?.Undo.CanExecute(null) ?? false).ObservesProperty(() => DocumentManager.ActiveDocument).
                ObservesProperty(() => DocumentManager.ActiveDocument.IsModified), new("Control+Z")));
            
            SubItems.Add(new ModuleMenuItem("_Redo", new DelegateCommand(() => DocumentManager.ActiveDocument?.Redo.Execute(null),
                () => DocumentManager.ActiveDocument?.Redo.CanExecute(null) ?? false).ObservesProperty(() => DocumentManager.ActiveDocument).
                ObservesProperty(() => DocumentManager.ActiveDocument.IsModified), new("Control+Y")));
            
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}