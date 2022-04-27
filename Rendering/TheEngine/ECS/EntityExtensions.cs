using System;
using System.Threading;

namespace TheEngine.ECS
{
    public static class EntityExtensions
    {
        public static void ForEach<T>(this Archetype archetype, Action<IChunkDataIterator, int, int, ComponentDataAccess<T>> process) where T : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var access = itr.DataAccess<T>();
                process(itr, 0, itr.Length, access);
            }
        }
        
        public static void ForEach<T1, T2>(this Archetype archetype, 
            System.Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                process(itr, 0, itr.Length, itr.DataAccess<T1>(), itr.DataAccess<T2>());
            }
        }
        
        public static void ForEach<T1, T2, T3>(this Archetype archetype, 
            System.Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
            where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                process(itr, 0, itr.Length, itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>());
            }
        }
        
        public static void ForEach<T1, T2, T3, T4>(this Archetype archetype, 
            System.Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
            where T3 : unmanaged, IComponentData
            where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                process(itr, 0, itr.Length, itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>());
            }
        }

        public static void ForEach<N1, T1>(this Archetype archetype, 
            System.Action<IChunkDataIterator, int, int, ComponentDataAccess<N1>, ManagedComponentDataAccess<T1>> process)
            where N1 : unmanaged, IComponentData
            where T1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                process(itr, 0, itr.Length, itr.DataAccess<N1>(), itr.ManagedDataAccess<T1>());
            }
        }
        
        private static void RunThreads(int start, int total, Action<int, int> action)
        {
            int threads = Environment.ProcessorCount;
            if (total < threads * 1000)
                threads = Math.Clamp(total / 1000, 1, threads);
            int perThread = total / threads;

            if (threads == 1)
            {
                action(0, total);
            }
            else
            {
                Thread[] alloc = new Thread[threads];
                for (int i = 0; i < threads; ++i)
                {
                    if (i == threads - 1)
                        perThread = total - start;
                    
                    var start1 = start;
                    var end = start + perThread;
                    alloc[i] = new Thread(() =>
                    {
                        action(start1, end);
                    });
                    alloc[i].Start();
                    start += perThread;
                }

                foreach (var t in alloc)
                    t.Join();   
            }
        }
        
        public static void ParallelForEach<T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>> process)
            where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var a1 = itr.DataAccess<T1>();
                RunThreads(0, itr.Length, (s, e) => process(itr, s, e, a1));
            }
        }
        
        public static void ParallelForEach<T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var a1 = itr.DataAccess<T1>();
                var a2 = itr.DataAccess<T2>();
                RunThreads(0, itr.Length, (s, e) => process(itr, s, e, a1, a2));
            }
        }
        
        public static void ParallelForEach<T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
            where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var a1 = itr.DataAccess<T1>();
                var a2 = itr.DataAccess<T2>();
                var a3 = itr.DataAccess<T3>();
                RunThreads(0, itr.Length, (s, e) => process(itr, s, e, a1, a2, a3));
            }
        }
        
        
        public static void ParallelForEach<T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, 
                ComponentDataAccess<T2>, 
                ComponentDataAccess<T3>, 
                ComponentDataAccess<T4>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
            where T3 : unmanaged, IComponentData
            where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var a1 = itr.DataAccess<T1>();
                var a2 = itr.DataAccess<T2>();
                var a3 = itr.DataAccess<T3>();
                var a4 = itr.DataAccess<T4>();
                RunThreads(0, itr.Length, (s, e) => process(itr, s, e, a1, a2, a3, a4));
            }
        }
        
        
        
        public static void ParallelForEach<T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T1>, 
                ComponentDataAccess<T2>, 
                ComponentDataAccess<T3>, 
                ComponentDataAccess<T4>, 
                ComponentDataAccess<T5>> process)
            where T1 : unmanaged, IComponentData
            where T2 : unmanaged, IComponentData
            where T3 : unmanaged, IComponentData
            where T4 : unmanaged, IComponentData
            where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var a1 = itr.DataAccess<T1>();
                var a2 = itr.DataAccess<T2>();
                var a3 = itr.DataAccess<T3>();
                var a4 = itr.DataAccess<T4>();
                var a5 = itr.DataAccess<T5>();
                RunThreads(0, itr.Length, (s, e) => process(itr, s, e, a1, a2, a3, a4, a5));
            }
        }
    }
}