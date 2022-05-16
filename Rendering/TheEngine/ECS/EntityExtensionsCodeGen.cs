
namespace TheEngine.ECS;

public static partial class EntityExtensions
{


        public static void ForEach<N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>> process)where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEach<N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>> process)where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEach<N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEach<N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEach<T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>> process)where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>()
                    );
            }
        }

        public static void ParallelForEach<T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>> process)where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>()
));
            }
        }

        public static void ForEach<T0, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEach<T0, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<T0, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEach<T0, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<T0, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEach<T0, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEach<T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>()
));
            }
        }

        public static void ForEach<T0, T1, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<T0, T1, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<T0, T1, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEach<T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>()
));
            }
        }

        public static void ForEach<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<T0, T1, T2, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<T0, T1, T2, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>> process)where T0 : unmanaged, IComponentData
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
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>> process)where T0 : unmanaged, IComponentData
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
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4, T5, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4, T5, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<T0, T1, T2, T3, T4, T5, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEach<T0, T1, T2, T3, T4, T5, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

}