namespace TheEngine.Utils;

public static class TaskExtensions
{
    public static void FireAndForget(this ValueTask t)
    {
        t.AsTask().ContinueWith(x => Console.WriteLine(x.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }
}