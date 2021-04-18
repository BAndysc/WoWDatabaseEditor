using System;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class StatusBar : BindableBase, IStatusBar
    {
        private readonly TasksViewModel tasksViewModel;
        private readonly IMainThread mainThread;

        public StatusBar(TasksViewModel tasksViewModel, IMainThread mainThread)
        {
            this.tasksViewModel = tasksViewModel;
            this.mainThread = mainThread;
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
            mainThread.Dispatch(() =>
            {
                var time = DateTime.Now.ToString("T");
                CurrentNotification = new PlainNotification(notification.Type,
                    $"[{time}] {notification.Message}",
                    notification.ClickCommand);
            });
        }
    }
}