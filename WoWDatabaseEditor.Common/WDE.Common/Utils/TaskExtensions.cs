using System;
using System.Threading.Tasks;

namespace WDE.Common.Utils
{
    public static class TaskExtensions
    {
        public static Task ListenErrors(this Task t)
        {
            return t.ContinueWith(e =>
            {
                Console.WriteLine(e.Exception);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}