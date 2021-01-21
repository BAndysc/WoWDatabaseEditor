using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class NewConditionViewModel : INotifyPropertyChanged
    {
        private SmartEvent @event;

        public SmartEvent Event
        {
            get => @event;
            set
            {
                if (@event != null)
                    @event.PropertyChanged -= EventOnPropertyChanged;
                @event = value;
                if (@event != null)
                    @event.PropertyChanged += EventOnPropertyChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private void EventOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SmartEvent.IsSelected))
                OnPropertyChanged(nameof(IsSelected));
        }

        public bool IsSelected { get => Event?.IsSelected ?? false;
            set { }
        } 

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}