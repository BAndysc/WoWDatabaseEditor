using System;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.MVVM;
using WoWDatabaseEditorCore.Services.ProblemsTool;
using WoWDatabaseEditorCore.ViewModels;

namespace WoWDatabaseEditorCore.Managers
{
    [AutoRegister]
    [SingleInstance]
    public class StatusBar : ObservableBase, IStatusBar
    {
        private readonly TasksViewModel tasksViewModel;
        private readonly IMainThread mainThread;

        public StatusBar(Lazy<IDocumentManager> documentManager,
            TasksViewModel tasksViewModel, 
            IEventAggregator eventAggregator,
            IMainThread mainThread)
        {
            this.tasksViewModel = tasksViewModel;
            this.mainThread = mainThread;

            AutoDispose(eventAggregator.GetEvent<AllModulesLoaded>().Subscribe(() =>
            {
                var problemsViewModel = documentManager.Value.GetTool<ProblemsViewModel>();
                Link(problemsViewModel, t => t.TotalProblems, () => TotalProblems);
            }, true));
            
            OpenProblemTool = new DelegateCommand(() => documentManager.Value.OpenTool<ProblemsViewModel>());
        }

        public ICommand OpenProblemTool { get; }

        public int TotalProblems { get; set; }
        
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