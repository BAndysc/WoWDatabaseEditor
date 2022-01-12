using System;
using System.Threading.Tasks;

namespace WDE.Common.Tasks
{
    public interface IMainThread
    {
        void Delay(System.Action action, TimeSpan delay);
        void Dispatch(System.Action action);
        Task Dispatch(System.Func<Task> action);
    }
}