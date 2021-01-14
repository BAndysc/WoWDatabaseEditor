using System.Threading.Tasks;

namespace WDE.Common.Tasks
{
    /**
     * This interface represent Asynchronous task, that WILL be fired on main thread
     * keep in mind that the task will run asynchronously along with other code
     */
    public interface IAsyncTask : ITask
    {
        Task Run(ITaskProgress progress);
    }
}