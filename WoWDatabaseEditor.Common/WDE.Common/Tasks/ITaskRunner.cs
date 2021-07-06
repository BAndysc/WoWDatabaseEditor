using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Tasks
{
    [UniqueProvider]
    public interface ITaskRunner
    {
        Task ScheduleTask(IThreadedTask threadedTask);
        Task ScheduleTask(IAsyncTask task);
        Task ScheduleTask(string name, Func<ITaskProgress, Task> task);
        Task ScheduleTask(string name, Func<Task> task);
    }
}