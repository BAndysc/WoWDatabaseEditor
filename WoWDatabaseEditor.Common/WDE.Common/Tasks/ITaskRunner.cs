using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Tasks
{
    [UniqueProvider]
    public interface ITaskRunner
    {
        void ScheduleTask(IThreadedTask threadedTask);
        void ScheduleTask(IAsyncTask task);
        void ScheduleTask(string name, Func<ITaskProgress, Task> task);
        void ScheduleTask(string name, Func<Task> task);
    }
}