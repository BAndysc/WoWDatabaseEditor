using System.Collections.Concurrent;

namespace TheEngine.Utils;

internal class NaiveThreadPool : System.IDisposable
{
    internal static NaiveThreadPool Pool = new();
    private Thread[] threads;
    private SemaphoreSlim[] semaphores;
    private ManualResetEventSlim[] resetEvents;
    private Action<int>? task;
    private volatile bool running;

    public static int ThreadCount => Environment.ProcessorCount;

    private NaiveThreadPool()
    {
        threads = new Thread[Environment.ProcessorCount];
        semaphores = new SemaphoreSlim[Environment.ProcessorCount];
        resetEvents = new ManualResetEventSlim[Environment.ProcessorCount];

        running = true;
        for (int i = 0; i < threads.Length; i++)
        {
            semaphores[i] = new SemaphoreSlim(0);
            resetEvents[i] = new ManualResetEventSlim(false);
            int index = i;
            threads[i] = new Thread(() => Run(index));
            threads[i].Name = "NaiveThreadPoolThread" + i;
            threads[i].Start();
        }
    }

    public void Dispose()
    {
        running = false;
        task = null;
        for (int i = 0; i < semaphores.Length; ++i)
            semaphores[i].Release();
    }

    private void Run(int index)
    {
        while (running)
        {
            try
            {
                semaphores[index].Wait();
                task?.Invoke(index);
                resetEvents[index].Set();
            }
            catch (Exception e)
            {
                if (running)
                {
                    Console.WriteLine("ERROR, Operation cancelled, but thread still running!" + e);
                    throw new Exception("ERROR, Operation cancelled, but thread still running!", e);
                }
                return;
            }
        }
        Console.WriteLine("Work thead " + index + " stopped.");
    }

    public void InvokeParallel(Action<int> action, int count)
    {
        if (count != Environment.ProcessorCount)
            throw new Exception("This is a naive thread pool, it can't really handle parallel tasks.");

        task = action;

        for (int i = 0; i < semaphores.Length; ++i)
            semaphores[i].Release();

        for (int i = 0; i < resetEvents.Length; ++i)
            resetEvents[i].Wait();

        for (int i = 0; i < resetEvents.Length; ++i)
            resetEvents[i].Reset();

        task = null;
    }
}
