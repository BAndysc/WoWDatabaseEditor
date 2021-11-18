using System.Collections.Generic;

namespace TheEngine.ECS
{
    public interface IEntityManager
    {
        Entity CreateEntity(Archetype archetype);
        void DestroyEntity(Entity entity);
        bool Exist(Entity entity);
        ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData;
        IEnumerable<IChunkDataIterator> ArchetypeIterator(Archetype archetype);
        IComponentTypeData TypeData<T>() where T : unmanaged, IComponentData;
        Archetype NewArchetype();
    }
}