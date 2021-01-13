using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Tasks
{
    [UniqueProvider]
    public interface ITaskRunner
    {
        void ScheduleTask(string taskName, Task task);
    }
}