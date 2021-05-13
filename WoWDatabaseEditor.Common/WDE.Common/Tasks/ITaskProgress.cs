using System;

namespace WDE.Common.Tasks
{
    /**
     * ITaskProgress represents progress of waiting/started/finished task.
     * Updated event should be ALWAYS called on main thread!
     */
    public interface ITaskProgress
    {
        TaskState State { get; }
        int CurrentProgress { get; }
        int MaxProgress { get; }
        string? CurrentTask { get; }
        
        void Report(int currentProgress, int maxProgress, string? currentTask);
        void ReportFinished();
        void ReportFail();
        event Action<ITaskProgress> Updated;
    }
}