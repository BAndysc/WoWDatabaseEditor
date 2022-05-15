using System.Collections.Generic;

namespace TheEngine.ECS
{
    internal class EntityDataManager : System.IDisposable
    {
        private readonly Dictionary<ulong, ChunkDataManager> archetypeToData = new();

        internal void AddEntity(Entity entity, Archetype archetype)
        {
            var hash = archetype.Hash;
            if (!archetypeToData.TryGetValue(hash, out var data))
                data = archetypeToData[hash] = new(archetype);
            data.AddEntity(entity);
        }

        internal void RemoveEntity(Entity entity, ulong archetypeBitMask)
        {
            archetypeToData[archetypeBitMask].RemoveEntity(entity);
        }

        public void MoveEntity(Entity entity, Archetype oldArchetype, Archetype newArchetype)
        {
            AddEntity(entity, newArchetype);

            var oldData = archetypeToData[oldArchetype.Hash];
            var newData = archetypeToData[newArchetype.Hash];
            
            foreach (var component in oldArchetype.Components)
                newData.UnsafeCopy(entity, oldData, component);
            
            foreach (var component in oldArchetype.ManagedComponents)
                newData.UnsafeCopy(entity, oldData, component);
            
            RemoveEntity(entity, oldArchetype.Hash);
        }

        internal IEnumerable<ChunkDataManager> Archetypes => archetypeToData.Values;
        
        internal ChunkDataManager this[ulong archetypeBitMask] => archetypeToData[archetypeBitMask];

        public void Dispose()
        {
            foreach (var a in Archetypes)
                a.Dispose();
            archetypeToData.Clear();
        }
    }
}