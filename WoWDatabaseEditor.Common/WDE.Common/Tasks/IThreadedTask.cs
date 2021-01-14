namespace WDE.Common.Tasks
{
    /**
     * IThreadedTask represents a task that can be synchronous, but will be run on a separate thread
     */
    public interface IThreadedTask : ITask
    {
        void Run(ITaskProgress progress);
        void FinishMainThread();
    }
}