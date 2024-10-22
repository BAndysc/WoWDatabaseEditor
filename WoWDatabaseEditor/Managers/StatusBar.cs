using System.ComponentModel;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public partial class StatusBar : ObservableBase, IStatusBar
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
    
    [FallbackAutoRegister]
    internal class NullConnectionsStatusBarItem : IConnectionsStatusBarItem
    {
        public int OpenedConnections => 0;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
