using System.Reactive.Linq;
using Dock.Model.ReactiveUI.Controls;
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
}