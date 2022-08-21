using System.ComponentModel;
using System.Runtime.CompilerServices;
using WDE.Common.Annotations;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Editor.ViewModels
{
    public class NewActionViewModel : INotifyPropertyChanged
    {
        private EventAiEvent? @event;

        public EventAiEvent? Event
        {
            get => @event;
            set
            {
                @event = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}