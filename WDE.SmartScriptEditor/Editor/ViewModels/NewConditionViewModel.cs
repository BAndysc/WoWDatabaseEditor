using WDE.MVVM;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class NewConditionViewModel : ObservableBase
    {
        private SmartEvent @event;
        public SmartEvent Event
        {
            get => @event;
            set
            {
                Dispose();
                Watch(@event, e => e.IsSelected, nameof(IsSelected));
                @event = value;
                RaisePropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => @event?.IsSelected ?? false;
            set { }
        }
    }
}