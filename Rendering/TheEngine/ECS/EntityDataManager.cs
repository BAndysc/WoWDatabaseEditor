using System.Collections.Generic;

namespace TheEngine.ECS
{
    internal class EntityDataManager : System.IDisposable
    {
        private readonly Dictionary<int, ChunkDataManager> archetypeToData = new();

        internal void AddEntity(Entity entity, Archetype archetype)
        {
            if (!archetypeToData.TryGetValue(archetype.ComponentBitMask, out var data))
                data = archetypeToData[archetype.ComponentBitMask] = new(archetype);
            data.AddEntity(entity);
        }

        internal void RemoveEntity(Entity entity, int archetypeBitMask)
        {
            archetypeToData[archetypeBitMask].RemoveEntity(entity);
        }

        internal IEnumerable<ChunkDataManager> Archetypes => archetypeToData.Values;
        
        internal ChunkDataManager this[int archetypeBitMask] => archetypeToData[archetypeBitMask];

        public void Dispose()
        {
            foreach (var a in Archetypes)
                a.Dispose();
            archetypeToData.Clear();
        }
    }
}