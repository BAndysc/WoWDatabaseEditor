using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;

namespace TheEngine.ECS
{
    public interface IComponentTypeData<T> : IComponentTypeData where T : unmanaged, IComponentData
    {}
    
    public interface IManagedComponentTypeData<T> : IManagedComponentTypeData where T : class, IManagedComponentData
    {}

    internal class EntityManager : IEntityManager, System.IDisposable
    {
        private readonly EntityDataManager dataManager = new();
        private readonly List<Entity> freeEntities = new();
        private Entity[] entities = new Entity[1];
        private ulong[] entitiesArchetype = new ulong[1];
        private uint used;
        private readonly Dictionary<System.Type, int> typeToIndexMapping = new();
        private readonly Dictionary<System.Type, int> typeToManagedIndexMapping = new();
        private readonly Dictionary<ulong, Archetype> archetypes = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeIfNeeded()
        {
            if (entities.Length <= used)
            {
                Array.Resize(ref entities, entities.Length * 2 + 1);
                Array.Resize(ref entitiesArchetype, entities.Length);
            }
        }
        
        public Entity CreateEntity(Archetype archetype)
        {
            Entity newEntity;
            if (freeEntities.Count > 0)
            {
                var reuseEntity = freeEntities[^1];
                freeEntities.RemoveAt(freeEntities.Count - 1);
                newEntity = new Entity(reuseEntity.Id, reuseEntity.Version + 1);
            }
            else
            {
                ResizeIfNeeded();
                newEntity = new Entity(used++, 1);
            }
            entities[newEntity.Id] = newEntity;
            entitiesArchetype[newEntity.Id] = archetype.Hash;
            dataManager.AddEntity(newEntity, archetype);
            return newEntity;
        }

        public void AddComponent<T>(Entity entity, in T component) where T : unmanaged, IComponentData
        {
            ulong currentArchetypeHash = entitiesArchetype[entity.Id];
            var componentTypeData = TypeData<T>();

            var entityAlreadyHasComponent = (currentArchetypeHash & componentTypeData.GlobalHash) != 0;
            if (entityAlreadyHasComponent)
            {
                Console.WriteLine("The entity has already the component, consider using GetComponent<T>() = value for more performance");
            }
            else
            {
                var oldArchetype = archetypes[currentArchetypeHash];
                var newArchetype = oldArchetype.WithComponentData<T>();
                dataManager.MoveEntity(entity, oldArchetype, newArchetype);
                entitiesArchetype[entity.Id] = newArchetype.Hash;
            }
            GetComponent<T>(entity) = component;
        }
        
        public void AddManagedComponent<T>(Entity entity, T component) where T : class, IManagedComponentData
        {
            ulong currentArchetypeHash = entitiesArchetype[entity.Id];
            var componentTypeData = ManagedTypeData<T>();

            var entityAlreadyHasComponent = (currentArchetypeHash & componentTypeData.GlobalHash) != 0;
            if (entityAlreadyHasComponent)
            {
                Console.WriteLine("The entity has already the component, consider using SetManagedComponent<T>(value) for more performance");
            }
            else
            {
                var oldArchetype = archetypes[currentArchetypeHash];
                var newArchetype = oldArchetype.WithManagedComponentData<T>();
                dataManager.MoveEntity(entity, oldArchetype, newArchetype);
                entitiesArchetype[entity.Id] = newArchetype.Hash;
            }
            SetManagedComponent<T>(entity, component);
        }

        public void DestroyEntity(Entity entity)
        {
            if (entities[entity.Id].Version != entity.Version)
                throw new Exception("Double remove entity, that's not allowed!");
            var archetypeHash = entitiesArchetype[entity.Id];
            dataManager.RemoveEntity(entity, archetypeHash);
            freeEntities.Add(entity);
            entities[entity.Id] = Entity.Empty;
            entitiesArchetype[entity.Id] = 0;
        }

        public bool Exist(Entity entity)
        {
            return entities.Length > entity.Id && entities[entity.Id] == entity;
        }

        public ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            return ref dataManager[entitiesArchetype[entity.Id]].DataAccess<T>()[entity];
        }

        public T GetManagedComponent<T>(Entity entity) where T : IManagedComponentData
        {
            return dataManager[entitiesArchetype[entity.Id]].ManagedDataAccess<T>()[entity];
        }
        
        public T SetManagedComponent<T>(Entity entity, T value) where T : IManagedComponentData
        {
            dataManager[entitiesArchetype[entity.Id]].ManagedDataAccess<T>()[entity] = value;
            return value;
        }

        public bool Is(Entity entity, Archetype archetype)
        {
            return (entitiesArchetype[entity.Id] & archetype.Hash) == archetype.Hash;
        }

        public void InstallArchetype(Archetype archetype)
        {
            archetypes[archetype.Hash] = archetype;
        }

        public IEnumerable<IChunkDataIterator> ArchetypeIterator(Archetype archetype)
        {
            foreach (var a in dataManager.Archetypes)
            {
                if (a.Archetype.Contains(archetype))
                    yield return a;
            }
        }

        public IComponentTypeData TypeData<T>() where T : unmanaged, IComponentData
        {
            if (!typeToIndexMapping.TryGetValue(typeof(T), out var index))
                index = typeToIndexMapping[typeof(T)] = typeToIndexMapping.Count;
            if (index >= 32)
                throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            return new ComponentTypeData<T>(index);
        }

        public IManagedComponentTypeData ManagedTypeData<T>() where T : class, IManagedComponentData
        {
            if (!typeToManagedIndexMapping.TryGetValue(typeof(T), out var index))
                index = typeToManagedIndexMapping[typeof(T)] = typeToManagedIndexMapping.Count;
            if (index >= 32)
                throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            return new ManagedComponentTypeData<T>(index);
        }

        public Archetype NewArchetype()
        {
            return new Archetype(this);
        }

        public void Dispose()
        {
            freeEntities.Clear();
            entities = null!;
            entitiesArchetype = null!;
            dataManager.Dispose();
        }
    }
}