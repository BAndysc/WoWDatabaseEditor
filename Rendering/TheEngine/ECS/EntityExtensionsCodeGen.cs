
namespace TheEngine.ECS;

public static partial class EntityExtensions
{


        public static void ForEach<N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>> process)where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRO<N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRO<N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRO<N0, T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>()
                    );
            }
        }

        public static void ParallelForEachRO<N0, T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>()
));
            }
        }

        public static void ForEachROO<N0, T0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachROO<N0, T0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachROO<N0, T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEachROO<N0, T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>()
));
            }
        }

        public static void ForEachROOO<N0, T0, T1, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachROOO<N0, T0, T1, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachROOO<N0, T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachROOO<N0, T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachROOOO<N0, T0, T1, T2, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachROOOO<N0, T0, T1, T2, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N1>?> process)where N0 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEach<N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRO<N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRO<N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRO<N0, N1, T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>()
                    );
            }
        }

        public static void ParallelForEachRRO<N0, N1, T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>()
));
            }
        }

        public static void ForEachRROO<N0, N1, T0, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRROO<N0, N1, T0, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRROO<N0, N1, T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEachRROO<N0, N1, T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>()
));
            }
        }

        public static void ForEachRROOO<N0, N1, T0, T1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRROOO<N0, N1, T0, T1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRROOO<N0, N1, T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRROOO<N0, N1, T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRROOOO<N0, N1, T0, T1, T2, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRROOOO<N0, N1, T0, T1, T2, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N2>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRO<N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRO<N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRO<N0, N1, N2, T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>()
                    );
            }
        }

        public static void ParallelForEachRRRO<N0, N1, N2, T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>()
));
            }
        }

        public static void ForEachRRROO<N0, N1, N2, T0, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRROO<N0, N1, N2, T0, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRROO<N0, N1, N2, T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEachRRROO<N0, N1, N2, T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>()
));
            }
        }

        public static void ForEachRRROOO<N0, N1, N2, T0, T1, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<N0, N1, N2, T0, T1, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRROOO<N0, N1, N2, T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<N0, N1, N2, T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRRROOOO<N0, N1, N2, T0, T1, T2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRROOOO<N0, N1, N2, T0, T1, T2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T0>?, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N3>?> process)where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEach<T0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>> process)where T0 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>()
));
            }
        }

        public static void ForEachRO<T0, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRO<T0, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRO<T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEachRO<T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>()
));
            }
        }

        public static void ForEachROO<T0, T1, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachROO<T0, T1, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachROO<T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachROO<T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachROOO<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachROOO<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachROOO<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachROOO<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachROOOO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachROOOO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEach<T0, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRO<T0, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRO<T0, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRO<T0, N0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEachRRO<T0, N0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>()
));
            }
        }

        public static void ForEachRROO<T0, N0, T1, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRROO<T0, N0, T1, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRROO<T0, N0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRROO<T0, N0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRROOO<T0, N0, T1, T2, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRROOO<T0, N0, T1, T2, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRROOO<T0, N0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRROOO<T0, N0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRROOOO<T0, N0, T1, T2, T3, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRROOOO<T0, N0, T1, T2, T3, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N1>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRO<T0, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRO<T0, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRO<T0, N0, N1, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEachRRRO<T0, N0, N1, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>()
));
            }
        }

        public static void ForEachRRROO<T0, N0, N1, T1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRROO<T0, N0, N1, T1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRROO<T0, N0, N1, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRRROO<T0, N0, N1, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRRROOO<T0, N0, N1, T1, T2, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<T0, N0, N1, T1, T2, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRROOO<T0, N0, N1, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<T0, N0, N1, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRROOOO<T0, N0, N1, T1, T2, T3, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRROOOO<T0, N0, N1, T1, T2, T3, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N2>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRO<T0, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRO<T0, N0, N1, N2, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, N0, N1, N2, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>()
));
            }
        }

        public static void ForEachRRRROO<T0, N0, N1, N2, T1, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, N0, N1, N2, T1, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRROO<T0, N0, N1, N2, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, N0, N1, N2, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRRRROOO<T0, N0, N1, N2, T1, T2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, N0, N1, N2, T1, T2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRROOO<T0, N0, N1, N2, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, N0, N1, N2, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRRROOOO<T0, N0, N1, N2, T1, T2, T3, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRROOOO<T0, N0, N1, N2, T1, T2, T3, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T1>?, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEach<T0, T1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>()
));
            }
        }

        public static void ForEachRRO<T0, T1, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRO<T0, T1, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRO<T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRRO<T0, T1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRROO<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRROO<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRROO<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRROO<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRROOO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRROOO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRROOO<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRROOO<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRROOOO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRROOOO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRO<T0, T1, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRO<T0, T1, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRO<T0, T1, N0, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRRRO<T0, T1, N0, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRRROO<T0, T1, N0, T2, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRROO<T0, T1, N0, T2, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRROO<T0, T1, N0, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRROO<T0, T1, N0, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRROOO<T0, T1, N0, T2, T3, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<T0, T1, N0, T2, T3, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRROOO<T0, T1, N0, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<T0, T1, N0, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRROOOO<T0, T1, N0, T2, T3, T4, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRROOOO<T0, T1, N0, T2, T3, T4, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N1>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRO<T0, T1, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, T1, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRO<T0, T1, N0, N1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, T1, N0, N1, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRRRROO<T0, T1, N0, N1, T2, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, T1, N0, N1, T2, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRROO<T0, T1, N0, N1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, T1, N0, N1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRRROOO<T0, T1, N0, N1, T2, T3, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, T1, N0, N1, T2, T3, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRROOO<T0, T1, N0, N1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, T1, N0, N1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRROOOO<T0, T1, N0, N1, T2, T3, T4, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRROOOO<T0, T1, N0, N1, T2, T3, T4, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N2>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, N0, N1, N2, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, N0, N1, N2, T2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, N0, N1, N2, T2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, N0, N1, N2, T2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, N0, N1, N2, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, N0, N1, N2, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, N0, N1, N2, T2, T3, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, N0, N1, N2, T2, T3, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, N0, N1, N2, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, N0, N1, N2, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRRROOOO<T0, T1, N0, N1, N2, T2, T3, T4, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOOO<T0, T1, N0, N1, N2, T2, T3, T4, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T2>?, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N3>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>()
));
            }
        }

        public static void ForEachRRRO<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRO<T0, T1, T2, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRO<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRRO<T0, T1, T2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRROO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRROO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRROO<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRROO<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRROOO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRROOO<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRROOO<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRROOOO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRROOOO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRO<T0, T1, T2, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, T1, T2, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRO<T0, T1, T2, N0, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, T1, T2, N0, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRRROO<T0, T1, T2, N0, T3, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, T1, T2, N0, T3, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRROO<T0, T1, T2, N0, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, T1, T2, N0, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRROOO<T0, T1, T2, N0, T3, T4, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, T1, T2, N0, T3, T4, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRROOO<T0, T1, T2, N0, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, T1, T2, N0, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRROOOO<T0, T1, T2, N0, T3, T4, T5, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRROOOO<T0, T1, T2, N0, T3, T4, T5, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N1>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, T2, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, T2, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, T2, N0, N1, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, T2, N0, N1, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, T2, N0, N1, T3, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, T2, N0, N1, T3, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, T2, N0, N1, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, T2, N0, N1, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, T2, N0, N1, T3, T4, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, T2, N0, N1, T3, T4, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, T2, N0, N1, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, T2, N0, N1, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRROOOO<T0, T1, T2, N0, N1, T3, T4, T5, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOOO<T0, T1, T2, N0, N1, T3, T4, T5, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N2>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, N0, N1, N2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, N0, N1, N2, T3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, N0, N1, N2, T3, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, N0, N1, N2, T3, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, N0, N1, N2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, N0, N1, N2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, N0, N1, N2, T3, T4, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, N0, N1, N2, T3, T4, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, N0, N1, N2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, N0, N1, N2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRRROOOO<T0, T1, T2, N0, N1, N2, T3, T4, T5, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOOO<T0, T1, T2, N0, N1, N2, T3, T4, T5, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T3>?, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N3>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>()
));
            }
        }

        public static void ForEachRRRRO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, T1, T2, T3, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRO<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRRO<T0, T1, T2, T3, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRROO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRROO<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRROO<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRROOO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRROOO<T0, T1, T2, T3, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRROOO<T0, T1, T2, T3, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRROOOO<T0, T1, T2, T3, T4, T5, T6, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRROOOO<T0, T1, T2, T3, T4, T5, T6, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N0>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, T2, T3, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, T2, T3, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, T2, T3, N0, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, T2, T3, N0, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, T2, T3, N0, T4, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, T2, T3, N0, T4, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, T2, T3, N0, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, T2, T3, N0, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, T2, T3, N0, T4, T5, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, T2, T3, N0, T4, T5, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, T2, T3, N0, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, T2, T3, N0, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRROOOO<T0, T1, T2, T3, N0, T4, T5, T6, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOOO<T0, T1, T2, T3, N0, T4, T5, T6, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N1>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, T3, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, T3, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, T3, N0, N1, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, T3, N0, N1, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, T3, N0, N1, T4, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, T3, N0, N1, T4, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, T3, N0, N1, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, T3, N0, N1, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, T3, N0, N1, T4, T5, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, T3, N0, N1, T4, T5, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, T3, N0, N1, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, T3, N0, N1, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRROOOO<T0, T1, T2, T3, N0, N1, T4, T5, T6, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOOO<T0, T1, T2, T3, N0, N1, T4, T5, T6, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N2>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRRO<T0, T1, T2, T3, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRO<T0, T1, T2, T3, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRRO<T0, T1, T2, T3, N0, N1, N2, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRO<T0, T1, T2, T3, N0, N1, N2, T4>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>()
));
            }
        }

        public static void ForEachRRRRRRROO<T0, T1, T2, T3, N0, N1, N2, T4, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROO<T0, T1, T2, T3, N0, N1, N2, T4, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRROO<T0, T1, T2, T3, N0, N1, N2, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROO<T0, T1, T2, T3, N0, N1, N2, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRRRROOO<T0, T1, T2, T3, N0, N1, N2, T4, T5, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOO<T0, T1, T2, T3, N0, N1, N2, T4, T5, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRROOO<T0, T1, T2, T3, N0, N1, N2, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOO<T0, T1, T2, T3, N0, N1, N2, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRRROOOO<T0, T1, T2, T3, N0, N1, N2, T4, T5, T6, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOOO<T0, T1, T2, T3, N0, N1, N2, T4, T5, T6, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T4>?, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N3>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, T2, T3, T4, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRRO<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRRO<T0, T1, T2, T3, T4, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRROO<T0, T1, T2, T3, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRROO<T0, T1, T2, T3, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, T2, T3, T4, T5, T6, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, T2, T3, T4, T5, T6, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRROOO<T0, T1, T2, T3, T4, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOO<T0, T1, T2, T3, T4, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRROOOO<T0, T1, T2, T3, T4, T5, T6, T7, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRROOOO<T0, T1, T2, T3, T4, T5, T6, T7, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N0>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, T3, T4, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, T3, T4, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, T3, T4, N0, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, T3, T4, N0, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, T3, T4, N0, T5, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, T3, T4, N0, T5, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, T3, T4, N0, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, T3, T4, N0, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, T3, T4, N0, T5, T6, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, T3, T4, N0, T5, T6, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, T3, T4, N0, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, T3, T4, N0, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRRROOOO<T0, T1, T2, T3, T4, N0, T5, T6, T7, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOOO<T0, T1, T2, T3, T4, N0, T5, T6, T7, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N1>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, T5, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, T5, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, T5, T6, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, T5, T6, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRRRROOOO<T0, T1, T2, T3, T4, N0, N1, T5, T6, T7, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOOO<T0, T1, T2, T3, T4, N0, N1, T5, T6, T7, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N2>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, N2, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRO<T0, T1, T2, T3, T4, N0, N1, N2, T5>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, N2, T5, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, N2, T5, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROOO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRRRRROOOO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6, T7, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROOOO<T0, T1, T2, T3, T4, N0, N1, N2, T5, T6, T7, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T5>?, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N3>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, T3, T4, T5, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRRRO<T0, T1, T2, T3, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRO<T0, T1, T2, T3, T4, T5, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, T3, T4, T5, T6, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, T3, T4, T5, T6, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRRROO<T0, T1, T2, T3, T4, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROO<T0, T1, T2, T3, T4, T5, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, T3, T4, T5, T6, T7, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, T3, T4, T5, T6, T7, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRRROOO<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOO<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
));
            }
        }

        public static void ForEachRRRRRROOOO<T0, T1, T2, T3, T4, T5, T6, T7, T8, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N0>()
                    );
            }
        }

        public static void ParallelForEachRRRRRROOOO<T0, T1, T2, T3, T4, T5, T6, T7, T8, N0>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N0>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N0 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N0>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>()
));
            }
        }

        public static void ForEachRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, T6, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, T6, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, T6, T7, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, T6, T7, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
));
            }
        }

        public static void ForEachRRRRRRROOOO<T0, T1, T2, T3, T4, T5, N0, T6, T7, T8, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N1>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRROOOO<T0, T1, T2, T3, T4, T5, N0, T6, T7, T8, N1>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N1>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N1 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N1>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>()
));
            }
        }

        public static void ForEachRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, T6, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, T6, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
));
            }
        }

        public static void ForEachRRRRRRRROOOO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7, T8, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N2>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRROOOO<T0, T1, T2, T3, T4, T5, N0, N1, T6, T7, T8, N2>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N2>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N2 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N2>()
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
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
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>()
));
            }
        }

        public static void ForEachRRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, N2, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRRO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>()
));
            }
        }

        public static void ForEachRRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRROO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>()
));
            }
        }

        public static void ForEachRRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

        public static void ForEachRRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRROOO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7, T8>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>()
));
            }
        }

        public static void ForEachRRRRRRRRROOOO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7, T8, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    process(itr, 0, itr.Length, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N3>()
                    );
            }
        }

        public static void ParallelForEachRRRRRRRRROOOO<T0, T1, T2, T3, T4, T5, N0, N1, N2, T6, T7, T8, N3>(this Archetype archetype, 
            Action<IChunkDataIterator, int, int, ComponentDataAccess<T0>, ComponentDataAccess<T1>, ComponentDataAccess<T2>, ComponentDataAccess<T3>, ComponentDataAccess<T4>, ComponentDataAccess<T5>, ManagedComponentDataAccess<N0>, ManagedComponentDataAccess<N1>, ManagedComponentDataAccess<N2>, ComponentDataAccess<T6>?, ComponentDataAccess<T7>?, ComponentDataAccess<T8>?, ManagedComponentDataAccess<N3>?> process)where T0 : unmanaged, IComponentData
where T1 : unmanaged, IComponentData
where T2 : unmanaged, IComponentData
where T3 : unmanaged, IComponentData
where T4 : unmanaged, IComponentData
where T5 : unmanaged, IComponentData
where N0 : IManagedComponentData
where N1 : IManagedComponentData
where N2 : IManagedComponentData
where T6 : unmanaged, IComponentData
where T7 : unmanaged, IComponentData
where T8 : unmanaged, IComponentData
where N3 : IManagedComponentData
        {
            var entityManager = archetype.EntityManager;
            var iterator = entityManager.ArchetypeIterator(archetype);
            while (iterator.MoveNext())
            {
                var itr = iterator.Current;
                    RunThreads(0, itr.Length, (s, e) => process(itr, s, e, 
itr.DataAccess<T0>(), itr.DataAccess<T1>(), itr.DataAccess<T2>(), itr.DataAccess<T3>(), itr.DataAccess<T4>(), itr.DataAccess<T5>(), itr.ManagedDataAccess<N0>(), itr.ManagedDataAccess<N1>(), itr.ManagedDataAccess<N2>(), itr.OptionalDataAccess<T6>(), itr.OptionalDataAccess<T7>(), itr.OptionalDataAccess<T8>(), itr.OptionalManagedDataAccess<N3>()
));
            }
        }

}