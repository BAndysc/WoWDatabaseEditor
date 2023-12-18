using System;
using System.Threading.Tasks;

namespace WDE.Common.Tasks
{
    public interface IMainThread
    {
        System.IDisposable Delay(System.Action action, TimeSpan delay);
        void Dispatch(System.Action action);
        Task Dispatch(System.Func<Task> action);
        /// <summary>Starts a new timer.</summary>
        /// <param name="action">
        /// The method to call on timer tick. If the method returns false, the timer will stop.
        /// </param>
        /// <param name="interval">The interval at which to tick.</param>
        /// <returns>An <see cref="T:System.IDisposable" /> used to cancel the timer.</returns>
        IDisposable StartTimer(Func<bool> action, TimeSpan interval);
    }
}