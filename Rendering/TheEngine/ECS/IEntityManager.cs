using System;
using System.Collections.Generic;

namespace TheEngine.ECS
{
    public interface IEntityManager
    {
        Entity CreateEntity(Archetype archetype);
        Entity CreateEntity(Archetype archetype, string name);
        Entity CreateEntity(Archetype archetype, ReadOnlySpan<byte> nameUtf8);
        /**
         * Adds a component to entity, even if the component is NOT in the archetype
         * this will move the entity to a new archetype, which is an expensive operation
         * so you are not expected to do it every frame!!
         */
        void AddComponent<T>(Entity entity, in T component) where T : unmanaged, IComponentData;
        void AddManagedComponent<T>(Entity entity, T component) where T : class, IManagedComponentData;

        // Array component methods
        void AddArrayComponent<T>(Entity entity, in T component) where T : unmanaged, IComponentData;
        bool RemoveArrayComponent<T>(Entity entity, int componentIndex = 0) where T : unmanaged, IComponentData;
        Span<T> GetArrayComponents<T>(Entity entity) where T : unmanaged, IComponentData;

        void DestroyEntity(Entity entity);
        bool Exist(Entity entity);

        /**
         * Re-parents an entity. Pass Entity.Empty as parent to make it a root.
         * Maintained via the intrusive Relationship linked list, so this is O(1) and does
         * NOT move the entity to a new archetype (every entity already has Relationship).
         */
        void SetParent(Entity child, Entity parent);
        /** First root entity (Parent == Empty), or Entity.Empty if none. */
        Entity HierarchyFirstRoot { get; }
        /** Monotonic counter bumped on every structural hierarchy change. */
        int StructuralVersion { get; }
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
        ComponentArrayDataAccess<T> GetArrayDataAccessByEntity<T>(Entity entity) where T : unmanaged, IComponentData;
        
        Archetype RelationshipArchetype { get; }
    }
}