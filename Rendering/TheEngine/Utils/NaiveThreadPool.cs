using System.Collections.Concurrent;

namespace TheEngine.Utils;

internal class NaiveThreadPool : System.IDisposable
{
    internal static NaiveThreadPool Pool = new();
    private Thread[] threads;
    private BlockingCollection<(Action<int, int>, int, int)> tasks = new();
    private ManualResetEvent resetEvent = new(false);
    private int counter = 0;
    private CancellationTokenSource cts = new();
    private bool running = true;

    private NaiveThreadPool()
    {
        threads = new Thread[Environment.ProcessorCount];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(Run);
            threads[i].Start();
        }
    }

    public void Dispose()
    {
        running = false;
        cts.Cancel();
        tasks.Dispose();
        tasks = null!;
        resetEvent.Set();
        resetEvent.Dispose();
    }

    private void Run()
    {
        while (running)
        {
            try
            {
                var task = tasks.Take(cts.Token);
                task.Item1(task.Item2, threads.Length);
                Interlocked.Decrement(ref counter);
                resetEvent.Set();   
            }
            catch (OperationCanceledException)
            {
                if (running)
                    throw new Exception("ERROR, Operation cancelled, but thread still running!");
                return;
            }
            catch (ObjectDisposedException)
            {
                if (running)
                    throw new Exception("ERROR, Operation cancelled, but thread still running!");
                return;
            }
            catch (InvalidOperationException)
            {
                if (running)
                    throw new Exception("ERROR, Operation cancelled, but thread still running!");
                return;
            }
        }
    }

    public void InvokeParallel(Action<int, int> action, int count)
    {
        if (tasks.Count != 0)
            throw new Exception("This is a naive thread pool, it can't really handle parallel tasks.");
            
        counter = count;
            
        for (int i = 0; i < count; ++i)
        {
            tasks.Add((action, i, count));
        }

        do
        {
            if (counter != 0 && running)
                resetEvent.WaitOne();
        } while (counter != 0 && running);
    }
}
