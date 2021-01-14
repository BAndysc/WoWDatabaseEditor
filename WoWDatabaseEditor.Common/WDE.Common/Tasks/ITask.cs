namespace WDE.Common.Tasks
{
    /**
     * ITask represent abstract task, it doesn't have any method to run not doesn't guarantee any threading constraint
     *
     * if WaitForOtherTasks is true, then TaskRunner should respect that and do not run other task while this one is running
     */
    public interface ITask
    {
        string Name { get; }
        bool WaitForOtherTasks { get; }
    }
}