using System.Collections.Generic;
using TheEngine.Managers;

namespace TheEngine.ECS
{
    internal class EntityDataManager : System.IDisposable
    {
        private readonly StatsManager statsManager;
        private readonly Engine engine;
        private readonly Dictionary<ulong, int> archetypeToDataIndex = new();
        private readonly List<ChunkDataManager> data = new();

        public EntityDataManager(StatsManager statsManager, Engine engine)
        {
            this.statsManager = statsManager;
            this.engine = engine;
        }

        internal void AddEntity(Entity entity, Archetype archetype, bool invokeOnAddedHooks = true)
        {
            var hash = archetype.Hash;
            if (!archetypeToDataIndex.TryGetValue(hash, out var dataIndex))
            {
                data.Add(new ChunkDataManager(archetype, statsManager, engine));
                dataIndex = data.Count - 1;
                archetypeToDataIndex[hash] = dataIndex;
            }
            data[dataIndex].AddEntity(entity, invokeOnAddedHooks);
        }

        internal void RemoveEntity(Entity entity, ulong archetypeBitMask, bool isDestroyed)
        {
            data[archetypeToDataIndex[archetypeBitMask]].RemoveEntity(entity, isDestroyed);
        }

        public void MoveEntity(Entity entity, Archetype oldArchetype, Archetype newArchetype)
        {
            // existing components get overwritten by UnsafeCopy below, so don't re-fire OnAdded for them
            AddEntity(entity, newArchetype, invokeOnAddedHooks: false);

            var oldData = data[archetypeToDataIndex[oldArchetype.Hash]];
            var newData = data[archetypeToDataIndex[newArchetype.Hash]];
            
            foreach (var component in oldArchetype.Components)
                newData.UnsafeCopy(entity, oldData, component);
            
            foreach (var component in oldArchetype.ManagedComponents)
                newData.UnsafeCopy(entity, oldData, component);
            
            RemoveEntity(entity, oldArchetype.Hash, false);
        }

        internal ArchetypeIterator Archetypes => new ArchetypeIterator(this);
        
        internal ChunkDataManager this[ulong archetypeBitMask] => data[archetypeToDataIndex[archetypeBitMask]];

        public void Dispose()
        {
            foreach (var a in data)
                a.Dispose();
            data.Clear();
            archetypeToDataIndex.Clear();
        }

        public struct ArchetypeIterator
        {
            private readonly EntityDataManager dataManager;
            private readonly int count;
            private int index = -1;

            public ArchetypeIterator(EntityDataManager dataManager)
            {
                this.dataManager = dataManager;
                count = dataManager.data.Count;
            }
            
            public bool MoveNext()
            {
                index++;
                return index < count;
            }
            
            public ChunkDataManager Current => dataManager.data[index];
        }
    }
}