using System.Collections.Generic;

namespace TheEngine.ECS
{
    public interface IEntityManager
    {
        Entity CreateEntity(Archetype archetype);
        /**
         * Adds a component to entity, even if the component is NOT in the archetype
         * this will move the entity to a new archetype, which is an expensive operation
         * so you are not expected to do it every frame!!
         */
        void AddComponent<T>(Entity entity, in T component) where T : unmanaged, IComponentData;
        void AddManagedComponent<T>(Entity entity, T component) where T : class, IManagedComponentData;
        void DestroyEntity(Entity entity);
        bool Exist(Entity entity);
        ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData;
        T GetManagedComponent<T>(Entity entity) where T : IManagedComponentData;
        T SetManagedComponent<T>(Entity entity, T value) where T : IManagedComponentData;
        ChunkDataIterator ArchetypeIterator(Archetype archetype);
        IComponentTypeData TypeData<T>() where T : unmanaged, IComponentData;
        IComponentTypeData TypeData(Type t);
        IManagedComponentTypeData ManagedTypeData<T>() where T : class, IManagedComponentData;
        Archetype NewArchetype();
        bool Is(Entity entity, Archetype archetype);
        void InstallArchetype(Archetype archetype);
        bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData;
        bool HasManagedComponent<T>(Entity entity) where T : class, IManagedComponentData;
        ComponentDataAccess<T> GetDataAccessByEntity<T>(Entity entity) where T : unmanaged, IComponentData;
    }
}