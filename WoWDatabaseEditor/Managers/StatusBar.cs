using Prism.Mvvm;
using WDE.Common.Managers;

namespace WoWDatabaseEditor.Managers
{
    [WDE.Module.Attributes.AutoRegister, WDE.Module.Attributes.SingleInstance]
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