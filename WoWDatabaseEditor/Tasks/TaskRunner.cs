using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SmartFormat.Utilities;
using WDE.Common.Tasks;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Tasks
{
    [AutoRegister]
    [UniqueProvider]
    [SingleInstance]
    public class TaskRunner : ITaskRunner
    {
        private readonly IMainThread mainThread;

        public TaskRunner(IMainThread mainThread)
        {
            this.mainThread = mainThread;
        }
        
        public ObservableCollection<(ITask, ITaskProgress)> Tasks { get; } =
            new ObservableCollection<(ITask, ITaskProgress)>();

        private List<(TaskCompletionSource, ITask, ITaskProgress)> pendingTasks = new List<(TaskCompletionSource, ITask, ITaskProgress)>();
        private HashSet<ITask> activeTasks = new HashSet<ITask>();

        public event Action? ActivePendingTasksChanged;
        public int ActivePendingTasksCount => activeTasks.Count + pendingTasks.Count;

        public Task ScheduleTask(IThreadedTask threadedTask) => ScheduleTask(threadedTask, new TaskProgressSync(mainThread));

        public Task ScheduleTask(IAsyncTask task) => ScheduleTask(task, new TaskProgressAsync());

        public Task ScheduleTask(string name, Func<ITaskProgress, Task> task) => ScheduleTask(new AsyncTask(name, task));

        public Task ScheduleTask(string name, Func<Task> task) => ScheduleTask(new AsyncTask(name, async _ => await task()));

        private void AssertMainThread()
        {
            Debug.Assert(SynchronizationContext.Current != null);
        }

        private void ScheduleNow(TaskCompletionSource taskCompletionSource, ITask task, ITaskProgress progress)
        {
            AssertMainThread();
            activeTasks.Add(task);
            if (task is IThreadedTask syncTask)
            {
                Task.Run(() =>
                    {
                        progress.Report(0, 1, null);
                        syncTask.Run(progress);
                    })
                    .ContinueWith(t =>
                    {
                        mainThread.Dispatch(() =>
                        {
                            if (t.IsCompletedSuccessfully)
                            {
                                syncTask.FinishMainThread();
                                progress.ReportFinished();
                            }
                            else if (t.IsFaulted)
                                progress.ReportFail();

                            activeTasks.Remove(task);
                            Tasks.Remove((task, progress));
                            if (t.Exception == null)
                                taskCompletionSource.SetResult();
                            else
                                taskCompletionSource.SetException(t.Exception);
                            PeekNextTask();
                        });
                    });
            }
            else if (task is IAsyncTask asyncTask)
            {
                Action execute = async () =>
                {
                    try
                    {
                        progress.Report(0, 1, null);
                        AssertMainThread();
                        await asyncTask.Run(progress);
                        AssertMainThread();
                        progress.ReportFinished();
                        taskCompletionSource.SetResult();
                    }
                    catch (Exception e)
                    {
                        AssertMainThread();
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e);
                        Console.WriteLine(e.StackTrace);
                        progress.ReportFail();
                        taskCompletionSource.SetException(e);
                    }
                    finally
                    {
                        AssertMainThread();
                        activeTasks.Remove(task);
                        Tasks.Remove((task, progress));
                        PeekNextTask();
                    }
                };
                execute();
            }
        }

        private Task ScheduleTask(ITask task, ITaskProgress progress)
        {
            TaskCompletionSource completionSource = new TaskCompletionSource();
            AssertMainThread();
            Tasks.Add((task, progress));
            if (task.WaitForOtherTasks && activeTasks.Count > 0)
                pendingTasks.Add((completionSource, task, progress));
            else
                ScheduleNow(completionSource, task, progress);
            ActivePendingTasksChanged?.Invoke();
            return completionSource.Task;
        }

        private void PeekNextTask()
        {
            AssertMainThread();
            if (pendingTasks.Count > 0)
            {
                var (completionSource, task, progress) = pendingTasks[0];
                if (task.WaitForOtherTasks && activeTasks.Count == 0)
                {
                    pendingTasks.RemoveAt(0);
                    ScheduleNow(completionSource, task, progress);
                }
            }

            ActivePendingTasksChanged?.Invoke();
        }
        
        private abstract class TaskProgress : ITaskProgress
        {
            public TaskState State { get; private set; } = TaskState.NotStarted;
            public int CurrentProgress { get; private set; } = 0;
            public int MaxProgress { get; private set; } = 1;
            public string? CurrentTask { get; private set; } = "Waiting";
            
            public DateTime? StartTime { get; private set; }

            public void Report(int currentProgress, int maxProgress, string? currentTask)
            {
                StartTime ??= DateTime.Now;
                
                State = TaskState.InProgress;
                CurrentProgress = currentProgress;
                MaxProgress = maxProgress;
                CurrentTask = currentTask;

                CallCallbacksOnMainThread();
            }

            public void ReportFinished()
            {
                var elapsed = StartTime.HasValue ? (DateTime.Now - StartTime.Value) : TimeSpan.Zero;
                
                State = TaskState.FinishedSuccess;
                if (MaxProgress <= 0)
                    MaxProgress = 1;
                CurrentProgress = MaxProgress;
                CurrentTask = "Done in " + elapsed.ToTimeString(TimeSpanFormatOptions.InheritDefaults, CommonLanguagesTimeTextInfo.English);
                CallCallbacksOnMainThread();
            }

            public void ReportFail()
            {
                State = TaskState.FinishedFailed;
                CurrentTask = "Error";
                CallCallbacksOnMainThread();
            }

            protected abstract void CallCallbacksOnMainThread();

            public event Action<ITaskProgress>? Updated;

            protected void InvokeUpdatedEvent()
            {
                Debug.Assert(SynchronizationContext.Current != null);
                Updated?.Invoke(this);
            }
        }

        private class TaskProgressSync : TaskProgress
        {
            private readonly IMainThread mainThread;

            public TaskProgressSync(IMainThread mainThread)
            {
                this.mainThread = mainThread;
            }
            
            protected override void CallCallbacksOnMainThread()
            {
                mainThread.Dispatch(InvokeUpdatedEvent);
            }
        }

        private class TaskProgressAsync : TaskProgress
        {
            protected override void CallCallbacksOnMainThread()
            {
                InvokeUpdatedEvent();
            }
        }

        private class AsyncTask : IAsyncTask
        {
            private readonly Func<ITaskProgress, Task> task;
            public string Name { get; }
            public bool WaitForOtherTasks => true;
            
            public AsyncTask(string name, Func<ITaskProgress, Task> task)
            {
                this.task = task;
                Name = name;
            }

            public async Task Run(ITaskProgress progress)
            {
                await task(progress);
            }
        }
    }
}