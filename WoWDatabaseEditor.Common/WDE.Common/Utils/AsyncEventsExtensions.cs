using System;
using System.Linq;
using System.Threading.Tasks;

namespace WDE.Common.Utils;

public static class AsyncEventsExtensions
{
    public static Task Raise<TSource, TEventArgs>(this Func<TSource, TEventArgs, Task>? handlers, TSource source, TEventArgs args)
        where TEventArgs : EventArgs
    {
        if (handlers != null)
        {
            return Task.WhenAll(handlers.GetInvocationList()
                .OfType<Func<TSource, TEventArgs, Task>>()
                .Select(h => h(source, args)));
        }

        return Task.CompletedTask;
    }
    
    public static async Task RaiseSerial<TSource, TEventArgs>(this Func<TSource, TEventArgs, Task>? handlers, TSource source, TEventArgs args)
        where TEventArgs : EventArgs
    {
        if (handlers != null)
        {
            foreach (var handler in handlers.GetInvocationList().OfType<Func<TSource, TEventArgs, Task>>())
            {
                await handler(source, args);
            }
        }
    }
}