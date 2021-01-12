using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class StatusBar : BindableBase, IStatusBar
    {
        private INotification? currentNotification;

        public INotification? CurrentNotification
        {
            get => currentNotification;
            private set => SetProperty(ref currentNotification, value);
        }

        public void PublishNotification(INotification notification)
        {
            CurrentNotification = notification;
        }
    }
}