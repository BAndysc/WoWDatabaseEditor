
using TheEngine.Utils;

namespace TheEngine.ECS;

public static partial class EntityExtensions
{
    
    private static void RunThreads(int start, int total, Action<int, int> action)
    {
        int threads = Environment.ProcessorCount;
        if (total < threads * 400)
            threads = Math.Clamp(total / 400, 1, threads);
        int perThread = total / threads;

        if (threads == 1)
        {
            action(0, total);
        }
        else
        {
            NaiveThreadPool.Pool.InvokeParallel((i, count) =>
            {
                int perThread = total / count;
                var start = i * perThread;
                if (i == threads - 1)
                    perThread = total - start;
                    
                action(start, start + perThread);
            }, threads);
        }
    }
}