using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class StatusBar : BindableBase, IStatusBar
    {
        private readonly TasksViewModel tasksViewModel;

        public StatusBar(TasksViewModel tasksViewModel)
        {
            this.tasksViewModel = tasksViewModel;
        }

        public TasksViewModel TasksViewModel => tasksViewModel;
        
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