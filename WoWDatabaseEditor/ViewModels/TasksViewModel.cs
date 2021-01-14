using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WoWDatabaseEditor.Tasks;

namespace WoWDatabaseEditor.ViewModels
{
    [UniqueProvider]
    [AutoRegister]
    [SingleInstance]
    public class TasksViewModel : BindableBase
    {
        public ObservableCollection<TaskViewModel> Tasks { get; } = new ObservableCollection<TaskViewModel>();

        public ICommand TogglePanelVisibility { get; }
        
        private int pendingTasksCount;
        public int PendingTasksCount
        {
            get => pendingTasksCount;
            private set => SetProperty(ref pendingTasksCount, value);
        }

        private bool isPanelVisible;
        public bool IsPanelVisible
        {
            get => isPanelVisible;
            set => SetProperty(ref isPanelVisible, value);
        }
        
        public TasksViewModel(TaskRunner taskRunner)
        {
            taskRunner.Tasks.CollectionChanged += TasksOnCollectionChanged;
            taskRunner.ActivePendingTasksChanged += () => PendingTasksCount = taskRunner.ActivePendingTasksCount;
            pendingTasksCount = taskRunner.ActivePendingTasksCount;
            foreach (var pair in taskRunner.Tasks)
                NewTask(pair);

            Tasks.CollectionChanged += (sender, args) =>
            {
                if (args.OldItems == null)
                    return;

                foreach (TaskViewModel vm in args.OldItems)
                    vm.Dispose();
            };

            TogglePanelVisibility = new DelegateCommand(() => IsPanelVisible = !IsPanelVisible);
        }

        private void TasksOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach ((ITask, ITaskProgress) pair in e.NewItems)
                {
                    NewTask(pair);
                }   
            }
        }

        private void NewTask((ITask, ITaskProgress) pair)
        {
            Tasks.Add(new TaskViewModel(this, pair.Item1, pair.Item2));
        }

        public class TaskViewModel : BindableBase, System.IDisposable
        {
            private readonly ITaskProgress taskProgress;
            public string TaskName { get; }
            public DelegateCommand ForgetTaskCommand { get; }

            public TaskViewModel(TasksViewModel parent, ITask task, ITaskProgress taskProgress)
            {
                this.taskProgress = taskProgress;
                ForgetTaskCommand = new DelegateCommand(() =>
                {
                    parent.Tasks.Remove(this);
                }, () => (state == TaskState.FinishedFailed || state == TaskState.FinishedSuccess));
                TaskName = task.Name;
                OnProgressUpdate(taskProgress);
                taskProgress.Updated += OnProgressUpdate;
            }

            private void OnProgressUpdate(ITaskProgress progress)
            {
                Debug.Assert(SynchronizationContext.Current != null);
                MaxValue = progress.MaxProgress;
                Progress = progress.CurrentProgress;
                CurrentTask = progress.CurrentTask;
                State = progress.State;
                ForgetTaskCommand.RaiseCanExecuteChanged();
            }
            
            private TaskState state;
            public TaskState State
            {
                get => state;
                set
                {
                    SetProperty(ref state, value);
                }
            }
            
            private double progress = -1;
            public double Progress
            {
                get => progress;
                set
                {
                    SetProperty(ref progress, value);
                    RaisePropertyChanged(nameof(InProgress));
                }
            }
            
            private double minValue;
            public double MinValue
            {
                get => minValue;
                set
                {
                    SetProperty(ref minValue, value);
                }
            }
            
            private double maxValue;
            public double MaxValue
            {
                get => maxValue;
                set
                {
                    SetProperty(ref maxValue, value);
                }
            }

            private string? currentTask = null;
            public string? CurrentTask
            {
                get => currentTask;
                set
                {
                    SetProperty(ref currentTask, value);
                }
            }
            
            public bool InProgress => Progress < MaxValue;

            public void Dispose()
            {
                taskProgress.Updated -= OnProgressUpdate;
            }
        }
    }
}