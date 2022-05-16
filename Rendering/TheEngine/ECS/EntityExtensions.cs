namespace TheEngine.ECS;

public static partial class EntityExtensions
{
    public static void ForEachRRRO<T1, T2, T3, N1>(this Archetype archetype, 
        System.Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N1>?> process)
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where N1 : IManagedComponentData
    {
        var entityManager = archetype.EntityManager;
        var iterator = entityManager.ArchetypeIterator(archetype);
        foreach (var itr in iterator)
        {
            process(itr, 0, itr.Length, itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalManagedDataAccess<N1>());
        }
    }
    
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
            Parallel.For(0, threads, (i, state) =>
            {
                var start = i * perThread;
                if (i == threads - 1)
                    perThread = total - start;
                    
                action(start, start + perThread);
            });
        }
    }
}