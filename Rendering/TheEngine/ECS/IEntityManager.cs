using System.Collections.Generic;

namespace TheEngine.ECS
{
    public interface IEntityManager
    {
        Entity CreateEntity(Archetype archetype);
        void DestroyEntity(Entity entity);
        bool Exist(Entity entity);
        ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData;
        T GetManagedComponent<T>(Entity entity) where T : IManagedComponentData;
        T SetManagedComponent<T>(Entity entity, T value) where T : IManagedComponentData;
        IEnumerable<IChunkDataIterator> ArchetypeIterator(Archetype archetype);
        IComponentTypeData TypeData<T>() where T : unmanaged, IComponentData;
        IManagedComponentTypeData ManagedTypeData<T>() where T : class, IManagedComponentData;
        Archetype NewArchetype();
        bool Is(Entity entity, Archetype archetype);
    }
}