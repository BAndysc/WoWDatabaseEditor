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