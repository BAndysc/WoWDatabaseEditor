using System.Collections.Generic;

namespace TheEngine.ECS
{
    internal class EntityDataManager : System.IDisposable
    {
        private readonly Dictionary<ulong, int> archetypeToDataIndex = new();
        private readonly List<ChunkDataManager> data = new();

        internal void AddEntity(Entity entity, Archetype archetype)
        {
            var hash = archetype.Hash;
            if (!archetypeToDataIndex.TryGetValue(hash, out var dataIndex))
            {
                data.Add(new ChunkDataManager(archetype));
                dataIndex = data.Count - 1;
                archetypeToDataIndex[hash] = dataIndex;
            }
            data[dataIndex].AddEntity(entity);
        }

        internal void RemoveEntity(Entity entity, ulong archetypeBitMask)
        {
            data[archetypeToDataIndex[archetypeBitMask]].RemoveEntity(entity);
        }

        public void MoveEntity(Entity entity, Archetype oldArchetype, Archetype newArchetype)
        {
            AddEntity(entity, newArchetype);

            var oldData = data[archetypeToDataIndex[oldArchetype.Hash]];
            var newData = data[archetypeToDataIndex[newArchetype.Hash]];
            
            foreach (var component in oldArchetype.Components)
                newData.UnsafeCopy(entity, oldData, component);
            
            foreach (var component in oldArchetype.ManagedComponents)
                newData.UnsafeCopy(entity, oldData, component);
            
            RemoveEntity(entity, oldArchetype.Hash);
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