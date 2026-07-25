using System;

namespace TheEngine.Utils;

public static class TaskExtensions
{
    public static void FireAndForget(this ValueTask t)
    {
        // Fast path: a ValueTask that already finished synchronously (e.g. the common "chunk already
        // loaded" case, called for every chunk in the load radius every frame). AsTask().ContinueWith
        // allocates a continuation Task + ContingentProperties on EVERY call - ruinous per-frame - so
        // observe completed tasks inline and only attach a continuation for genuinely pending ones.
        if (t.IsCompleted)
        {
            try
            {
                t.GetAwaiter().GetResult(); // consume the result / rethrow so the fault is observed
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return;
        }

        t.AsTask().ContinueWith(x => Console.WriteLine(x.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }
}