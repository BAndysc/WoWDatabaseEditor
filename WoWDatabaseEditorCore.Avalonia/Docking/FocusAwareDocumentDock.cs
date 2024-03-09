using Dock.Model.Mvvm.Controls;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Avalonia.Docking
{
    public class FocusAwareDocumentDock : DocumentDock
    {
        public FocusAwareDocumentDock()
        {
            this.ToObservable(t => t.IsActive).Where(w => w).SubscribeAction(_ =>
            {
                if (ActiveDockable is IDockableFocusable dockableFocusable)
                    dockableFocusable.OnFocus();
            });
        }
    }
    
    public class FocusAwareToolDock : ToolDock
    {
        public FocusAwareToolDock()
        {
            this.ToObservable(t => t.IsActive).Where(w => w).SubscribeAction(_ =>
            {
                if (ActiveDockable is IDockableFocusable dockableFocusable)
                    dockableFocusable.OnFocus();
            });
        }
    }
}