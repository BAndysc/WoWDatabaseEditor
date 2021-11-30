using WDE.Common.Tasks;

namespace DatabaseTester;

public class SyncTaskRunner : ITaskRunner
{
    private class DummyProgress : ITaskProgress
    {
        public TaskState State { get; set; }
        public int CurrentProgress { get; set; }
        public int MaxProgress { get; set; }
        public string? CurrentTask { get; set; }
        public void Report(int currentProgress, int maxProgress, string? currentTask) => Console.WriteLine(currentTask);
        public void ReportFinished() { }
        public void ReportFail() => throw new Exception("Task failed");
        public event Action<ITaskProgress>? Updated;
    }
    
    public Task ScheduleTask(IThreadedTask threadedTask)
    {
        threadedTask.Run(new DummyProgress());
        return Task.CompletedTask;
    }

    public Task ScheduleTask(IAsyncTask task)
    {
        task.Run(new DummyProgress()).Wait();
        return Task.CompletedTask;
    }

    public Task ScheduleTask(string name, Func<ITaskProgress, Task> task)
    {
        task(new DummyProgress()).Wait();
        return Task.CompletedTask;
    }

    public Task ScheduleTask(string name, Func<Task> task)
    {
        task().Wait();
        return Task.CompletedTask;
    }
}