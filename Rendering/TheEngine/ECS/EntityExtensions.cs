using System.Runtime.CompilerServices;
using TheEngine.Utils;

namespace TheEngine.ECS;

public static partial class EntityExtensions
{
    public delegate void ForEachThreadDelegate(object? data, object? data2, int threadIndex, int start, int end);

    // keeping them static to avoid allocations in closure below
    private static int threads = Environment.ProcessorCount;
    private static int perThread;
    private static ForEachThreadDelegate work;
    private static int totalWork;
    private static object? workData;
    private static object? workData2;
    private static unsafe void* jobPtr;

    public static int ParallelThreads = threads;

    // this method is not thread safe!
    private static void RunThreads(int start, int total, object? data, object? data2, ForEachThreadDelegate action)
    {
        //if (total < threads * 400)
        //    threads = Math.Clamp(total / 400, 1, threads);
        perThread = total / threads;
        work = action;
        totalWork = total;
        workData = data;
        workData2 = data2;

        if (total < 500)
        {
            action(data, data2, 0, 0, total);
        }
        else
        {
            NaiveThreadPool.Pool.InvokeParallel(static i =>
            {
                var thisPerThread = perThread;
                var thisStart = i * perThread;
                if (i == threads - 1)
                    thisPerThread = totalWork - thisStart;

                if (thisPerThread == 0)
                    return;

                work(workData, workData2, i, thisStart, thisStart + thisPerThread);
            }, threads);
        }

        workData = null;
        workData2 = null;
        work = null!;
    }

    public static unsafe void RunThreads<T>(int start, int total, ref T job) where T : IParallelJob
    {
        //if (total < threads * 400)
        //    threads = Math.Clamp(total / 400, 1, threads);
        perThread = total / threads;
        totalWork = total;

        if (total < 500)
        {
            job.Execute(0, 0, total);
        }
        else
        {
            jobPtr = Unsafe.AsPointer(ref job);

            NaiveThreadPool.Pool.InvokeParallel(static i =>
            {
                ref T j = ref Unsafe.AsRef<T>(jobPtr);

                var thisPerThread = perThread;
                var thisStart = i * perThread;
                if (i == threads - 1)
                    thisPerThread = totalWork - thisStart;

                if (thisPerThread == 0)
                    return;

                j.Execute(i, thisStart,thisStart + thisPerThread);
            }, threads);

            jobPtr = null;
        }
    }
}