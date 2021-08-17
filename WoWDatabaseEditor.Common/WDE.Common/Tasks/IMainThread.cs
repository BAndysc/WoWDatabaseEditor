using System.Threading.Tasks;

namespace WDE.Common.Tasks
{
    public interface IMainThread
    {
        void Dispatch(System.Action action);
        Task Dispatch(System.Func<Task> action);
    }
}