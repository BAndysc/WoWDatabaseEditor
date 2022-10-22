//#define DEBUG_ENTITY_CREATE_CALLSTACK

using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
#if DEBUG_ENTITY_CREATE_CALLSTACK
using System.Diagnostics;
#endif

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
        #if DEBUG_ENTITY_CREATE_CALLSTACK
        private StackTrace?[] entitySource = new StackTrace?[1];
        #endif
        private uint used;
        private readonly Dictionary<System.Type, int> typeToIndexMapping = new();
        private readonly Dictionary<System.Type, int> typeToManagedIndexMapping = new();
        private readonly Dictionary<ulong, Archetype> archetypes = new();

        internal EntityDataManager DataManager => dataManager;
        internal IEnumerable<Type> KnownTypes => typeToIndexMapping.Keys;
        internal IEnumerable<Type> KnownManagedTypes => typeToManagedIndexMapping.Keys;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeIfNeeded()
        {
            if (entities.Length <= used)
            {
                Array.Resize(ref entities, entities.Length * 2 + 1);
                Array.Resize(ref entitiesArchetype, entities.Length);
#if DEBUG_ENTITY_CREATE_CALLSTACK
                Array.Resize(ref entitySource, entities.Length);
#endif
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
#if DEBUG_ENTITY_CREATE_CALLSTACK
            entitySource[newEntity.Id] = new StackTrace(1, true);
#endif
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
#if DEBUG_ENTITY_CREATE_CALLSTACK
            entitySource[entity.Id] = null;
#endif
            entitiesArchetype[entity.Id] = 0;
        }

        public bool Exist(Entity entity)
        {
            return entities.Length > entity.Id && entities[entity.Id] == entity;
        }

        public ComponentDataAccess<T> GetDataAccessByEntity<T>(Entity entity) where T : unmanaged, IComponentData
        {
            return dataManager[entitiesArchetype[entity.Id]].DataAccess<T>();
        }
        
        public ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            return ref GetDataAccessByEntity<T>(entity)[entity];
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

        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            return (entitiesArchetype[entity.Id] & TypeData<T>().GlobalHash) != 0;
        }

        public bool HasManagedComponent<T>(Entity entity) where T : class, IManagedComponentData
        {
            return (entitiesArchetype[entity.Id] & ManagedTypeData<T>().GlobalHash) != 0;
        }

        public ChunkDataIterator ArchetypeIterator(Archetype archetype) => new ChunkDataIterator(this, archetype);

        public IComponentTypeData TypeData<T>() where T : unmanaged, IComponentData
        {
            if (!typeToIndexMapping.TryGetValue(typeof(T), out var index))
                index = typeToIndexMapping[typeof(T)] = typeToIndexMapping.Count;
            if (index >= 32)
                throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            return ComponentTypeData.Create<T>(index);
        }
        
        public IComponentTypeData TypeData(System.Type t)
        {
            if (!typeToIndexMapping.TryGetValue(t, out var index))
                index = typeToIndexMapping[t] = typeToIndexMapping.Count;
            if (index >= 32)
                throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            return new ComponentTypeData(t, index);
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
#if DEBUG_ENTITY_CREATE_CALLSTACK
            foreach (var trace in entitySource)
            {
                if (trace == null)
                    continue;
                Console.WriteLine("Entity not destroyed, created here: ");
                Console.WriteLine(trace);
            }
            entitySource = null!;
#endif
            freeEntities.Clear();
            entities = null!;
            entitiesArchetype = null!;
            dataManager.Dispose();
        }
        
        internal Archetype GetArchetypeByEntity(Entity entity)
        {
            return archetypes[entitiesArchetype[entity.Id]];
        }
        
        internal ChunkDataManager GetEntityDataManagerByEntity(Entity entity)
        {
            return dataManager[entitiesArchetype[entity.Id]];
        }
    }
    
    
    public struct ChunkDataIterator
    {
        private readonly Archetype archetype;
        private EntityDataManager.ArchetypeIterator iterator;

        internal ChunkDataIterator(EntityManager manager, Archetype archetype)
        {
            this.archetype = archetype;
            iterator = manager.DataManager.Archetypes;
        }

        public bool MoveNext()
        {
            while (iterator.MoveNext())
            {
                if (iterator.Current.Archetype.Contains(archetype))
                    return true;
            }

            return false;
        }
            
        public IChunkDataIterator Current => iterator.Current;
    }

}