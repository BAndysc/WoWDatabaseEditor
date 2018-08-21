using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prism.Mvvm;
using WDE.Common.Annotations;
using WDE.Common.History;

namespace WDE.Common.Managers
{
    public sealed class DocumentEditor : ContentControl, INotifyPropertyChanged
    {
        private Visibility _visibility;
        public string Title { get; set; }
        public ICommand Undo { get; set; }
        public ICommand Redo { get; set; }
        public ICommand Save { get; set; }

        public Visibility Visibilityt
        {
            get => _visibility;
            set { _visibility = value;
                OnPropertyChanged();
            }
        }

        public ICommand CloseCommand { get; set; }
        public bool CanClose { get; set; }
        
        public IHistoryManager History { get; set; }
        
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}