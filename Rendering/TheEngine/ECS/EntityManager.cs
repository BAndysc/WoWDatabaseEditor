//#define DEBUG_ENTITY_CREATE_CALLSTACK

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Managers;
using TheEngine.Utils;
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
        private readonly EntityDataManager dataManager;
        private readonly Engine engine;
        private readonly List<Entity> freeEntities = new();
        private Entity[] entities = new Entity[1];
        private ulong[] entitiesArchetype = new ulong[1];
        // per-entity chunk reference so the hot accessors (GetComponent & co) skip the
        // archetype-hash dictionary lookup; chunks are stable per archetype, so the cached
        // reference only changes when the entity's archetype changes
        private ChunkDataManager?[] entitiesChunk = new ChunkDataManager?[1];
        #if DEBUG_ENTITY_CREATE_CALLSTACK
        private StackTrace?[] entitySource = new StackTrace?[1];
        #endif
        private uint used;
        private readonly Dictionary<System.Type, int> typeToIndexMapping = new();
        private readonly Dictionary<System.Type, IComponentTypeData> typeToTypeDataMapping = new();
        private readonly Dictionary<System.Type, IManagedComponentTypeData> typeToManagedTypeDataMapping = new();
        private readonly Dictionary<System.Type, int> typeToManagedIndexMapping = new();
        private readonly Dictionary<ulong, Archetype> archetypes = new();

        // Head of the intrusive doubly-linked list of root entities (Parent == Empty).
        private Entity firstRoot = Entity.Empty;
        // Bumped on every structural hierarchy change (create, destroy, reparent). The
        // editor hierarchy view uses it to know when to re-flatten the visible tree.
        private int structuralVersion;

        /// <summary>First root entity, or <see cref="Entity.Empty"/> if there are none.
        /// Walk the tree via <see cref="Relationship.NextSibling"/> / <see cref="Relationship.FirstChild"/>.</summary>
        public Entity HierarchyFirstRoot => firstRoot;

        /// <summary>Monotonic counter incremented on any structural hierarchy change.</summary>
        public int StructuralVersion => structuralVersion;

        internal EntityDataManager DataManager => dataManager;
        internal IEnumerable<Type> KnownTypes => typeToIndexMapping.Keys;
        internal IEnumerable<Type> KnownManagedTypes => typeToManagedIndexMapping.Keys;

        public Archetype RelationshipArchetype { get; }

        public EntityManager(StatsManager statsManager, Engine engine)
        {
            this.engine = engine;
            dataManager = new(statsManager, engine);
            // preregister so they're addable in the scene view even if unused; also fixes their order
            TypeData<EntityName>();
            TypeData<LocalToWorld>();
            TypeData<Relationship>();
            TypeData<RenderEnabledBit>();
            TypeData<DirtyPosition>();
            TypeData<DisabledObjectBit>();
            TypeData<MeshRenderer>();
            TypeData<MeshBounds>();
            TypeData<WorldMeshBounds>();
            TypeData<CascadeShadowMap>();
            TypeData<Decal>();
            TypeData<Light>();
            TypeData<AmbientOcclusion>();

            RelationshipArchetype = NewArchetype().WithComponentData<CopyParentTransform>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeIfNeeded()
        {
            if (entities.Length <= used)
            {
                Array.Resize(ref entities, entities.Length * 2 + 1);
                Array.Resize(ref entitiesArchetype, entities.Length);
                Array.Resize(ref entitiesChunk, entities.Length);
#if DEBUG_ENTITY_CREATE_CALLSTACK
                Array.Resize(ref entitySource, entities.Length);
#endif
            }
        }

        public Entity CreateEntity(Archetype archetype, string name)
        {
            var entity = CreateEntityInternal(archetype);
            GetComponent<EntityName>(entity) = name;
            return entity;
        }

        public Entity CreateEntity(Archetype archetype, ReadOnlySpan<byte> nameUtf8)
        {
            var entity = CreateEntityInternal(archetype);
            GetComponent<EntityName>(entity) = nameUtf8;
            return entity;
        }

        public Entity CreateEntity(Archetype archetype)
        {
            var entity = CreateEntityInternal(archetype);
            ref var name = ref GetComponent<EntityName>(entity);
            Span<byte> nameBuffer = stackalloc byte[31];
            entity.TryFormat(nameBuffer, out var bytesWritten, "", null);
            nameBuffer[bytesWritten] = 0;
            name = (ReadOnlySpan<byte>)nameBuffer.Slice(0, bytesWritten + 1);
            return entity;
        }

        private Entity CreateEntityInternal(Archetype archetype)
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
            entitiesChunk[newEntity.Id] = dataManager[archetype.Hash];
            // Every entity starts life as a root (its Relationship is zeroed by AddEntity).
            LinkIntoList(newEntity, Entity.Empty);
            structuralVersion++;
            return newEntity;
        }

        public void AddComponent<T>(Entity entity, in T component) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            ulong currentArchetypeHash = entitiesArchetype[entity.Id];
            var componentTypeData = TypeData<T>();

            var entityAlreadyHasComponent = (currentArchetypeHash & componentTypeData.GlobalHash) != 0;
            if (entityAlreadyHasComponent)
            {
                Console.WriteLine($"The entity has already the component, consider using GetComponent<{typeof(T)}>() = value for more performance");
                GetComponent<T>(entity) = component;
            }
            else
            {
                var oldArchetype = archetypes[currentArchetypeHash];
                var newArchetype = oldArchetype.WithComponentData<T>();
                dataManager.MoveEntity(entity, oldArchetype, newArchetype);
                entitiesArchetype[entity.Id] = newArchetype.Hash;
                entitiesChunk[entity.Id] = dataManager[newArchetype.Hash];
                ref var comp = ref GetComponent<T>(entity);
                comp = component;
                componentTypeData.OnAddedAction?.Invoke(engine, entity, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref comp, 1)));
            }
        }
        
        public void AddManagedComponent<T>(Entity entity, T component) where T : class, IManagedComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
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
                entitiesChunk[entity.Id] = dataManager[newArchetype.Hash];
            }
            SetManagedComponent<T>(entity, component);
        }

        public void AddArrayComponent<T>(Entity entity, in T component) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            ulong currentArchetypeHash = entitiesArchetype[entity.Id];
            var componentTypeData = TypeData<T>();

            // unlike AddComponent, having the component already is the common case here (adding another element)
            if ((currentArchetypeHash & componentTypeData.GlobalHash) == 0)
            {
                var oldArchetype = archetypes[currentArchetypeHash];
                var newArchetype = oldArchetype.WithComponentData<T>();
                dataManager.MoveEntity(entity, oldArchetype, newArchetype);
                entitiesArchetype[entity.Id] = newArchetype.Hash;
                entitiesChunk[entity.Id] = dataManager[newArchetype.Hash];
            }
            GetEntityDataManagerByEntity(entity).AddArrayComponent<T>(entity, component);
        }

        public bool RemoveArrayComponent<T>(Entity entity, int componentIndex = 0) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return GetEntityDataManagerByEntity(entity).RemoveArrayComponent<T>(entity, componentIndex);
        }

        public Span<T> GetArrayComponents<T>(Entity entity) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return GetEntityDataManagerByEntity(entity).GetArrayComponents<T>(entity);
        }

        public void DestroyEntity(Entity entity)
        {
            if (entity == Entity.Empty)
                return;
            if (entities[entity.Id].Version != entity.Version)
                throw new Exception("Double remove entity, that's not allowed!");

            // Cascade: destroying an entity destroys its whole subtree. Re-read FirstChild every
            // iteration because each child's removal swap-relocates storage in the chunk.
            while (true)
            {
                var child = GetComponent<Relationship>(entity).FirstChild;
                if (child == Entity.Empty)
                    break;
                DestroyEntity(child);
            }

            // Detach from the hierarchy before removing storage, so neighbours' links stay valid.
            UnlinkFromList(entity);

            var archetypeHash = entitiesArchetype[entity.Id];
            dataManager.RemoveEntity(entity, archetypeHash, true);
            freeEntities.Add(entity);
            entities[entity.Id] = Entity.Empty;
#if DEBUG_ENTITY_CREATE_CALLSTACK
            entitySource[entity.Id] = null;
#endif
            entitiesArchetype[entity.Id] = 0;
            entitiesChunk[entity.Id] = null;
            structuralVersion++;
        }

        public bool Exist(Entity entity)
        {
            return entity != Entity.Empty && entities.Length > entity.Id && entities[entity.Id] == entity;
        }

        private void VerifyEntity(Entity entity)
        {
            if (!Exist(entity))
                throw new Exception("Entity does not exist or is empty, cannot get managed component");
        }

        public ComponentDataAccess<T> GetDataAccessByEntity<T>(Entity entity) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return entitiesChunk[entity.Id]!.DataAccess<T>();
        }

        public ComponentArrayDataAccess<T> GetArrayDataAccessByEntity<T>(Entity entity) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return entitiesChunk[entity.Id]!.ArrayDataAccess<T>();
        }

        public ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return ref GetDataAccessByEntity<T>(entity)[entity];
        }

        public T GetManagedComponent<T>(Entity entity) where T : IManagedComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return entitiesChunk[entity.Id]!.ManagedDataAccess<T>()[entity];
        }

        public T SetManagedComponent<T>(Entity entity, T value) where T : IManagedComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            var access = entitiesChunk[entity.Id]!.ManagedDataAccess<T>();
            access[entity] = value;
            return value;
        }

        public bool Is(Entity entity, Archetype archetype)
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return (entitiesArchetype[entity.Id] & archetype.Hash) == archetype.Hash;
        }

        public void InstallArchetype(Archetype archetype)
        {
            archetypes[archetype.Hash] = archetype;
        }

        public void SetParent(Entity child, Entity parent)
        {
#if DEBUG
            VerifyEntity(child);
            if (parent != Entity.Empty)
                VerifyEntity(parent);
            if (child == parent)
                throw new Exception("Cannot parent an entity to itself");
            // Reject cycles: parent must not be a descendant of child.
            for (var p = parent; p != Entity.Empty; p = GetComponent<Relationship>(p).Parent)
            {
                if (p == child)
                    throw new Exception("Cannot parent an entity to one of its descendants (cycle)");
            }
#endif
            UnlinkFromList(child);
            LinkIntoList(child, parent);
            structuralVersion++;
        }

        // Splices child at the head of parent's child list (or the root list when parent is Empty).
        // Assumes child is currently detached (Parent/siblings Empty), as left by UnlinkFromList
        // or a freshly created entity.
        private void LinkIntoList(Entity child, Entity parent)
        {
            var oldHead = parent == Entity.Empty ? firstRoot : GetComponent<Relationship>(parent).FirstChild;

            ref var childRel = ref GetComponent<Relationship>(child);
            childRel.Parent = parent;
            childRel.PrevSibling = Entity.Empty;
            childRel.NextSibling = oldHead;

            if (oldHead != Entity.Empty)
                GetComponent<Relationship>(oldHead).PrevSibling = child;

            if (parent == Entity.Empty)
            {
                firstRoot = child;
            }
            else
            {
                ref var parentRel = ref GetComponent<Relationship>(parent);
                parentRel.FirstChild = child;
                parentRel.ChildCount++;
            }
        }

        // Removes child from whatever list it currently belongs to (parent's children or the root
        // list) and clears its own parent/sibling links.
        private void UnlinkFromList(Entity child)
        {
            var rel = GetComponent<Relationship>(child);
            var parent = rel.Parent;
            var prev = rel.PrevSibling;
            var next = rel.NextSibling;

            if (prev != Entity.Empty)
                GetComponent<Relationship>(prev).NextSibling = next;
            else if (parent != Entity.Empty)
                GetComponent<Relationship>(parent).FirstChild = next;
            else
                firstRoot = next;

            if (next != Entity.Empty)
                GetComponent<Relationship>(next).PrevSibling = prev;

            if (parent != Entity.Empty)
                GetComponent<Relationship>(parent).ChildCount--;

            ref var childRel = ref GetComponent<Relationship>(child);
            childRel.Parent = Entity.Empty;
            childRel.PrevSibling = Entity.Empty;
            childRel.NextSibling = Entity.Empty;
        }

        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return (entitiesArchetype[entity.Id] & TypeData<T>().GlobalHash) != 0;
        }

        public bool HasManagedComponent<T>(Entity entity) where T : class, IManagedComponentData
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return (entitiesArchetype[entity.Id] & ManagedTypeData<T>().GlobalHash) != 0;
        }

        public ChunkDataIterator ArchetypeIterator(Archetype archetype) => new ChunkDataIterator(this, archetype);

        // GetComponent/HasComponent resolve type data on every call, so cache it per T in a static
        // generic: the Dictionary<Type,...> lookup becomes one field read + reference compare.
        // The entry pairs owner+data in one immutable object, so a reader never sees data from
        // another EntityManager instance (only relevant when several managers coexist, e.g. tests).
        private sealed class TypeDataCacheEntry
        {
            public required EntityManager Owner;
            public required IComponentTypeData Data;

            public TypeDataCacheEntry(EntityManager owner)
            {
                Owner = owner;
                owner.RegisterTypeData(this);
            }
        }

        private List<TypeDataCacheEntry> registeredUnmanagedCacheEntries = new();
        
        private void RegisterTypeData(TypeDataCacheEntry entry)
        {
            registeredUnmanagedCacheEntries.Add(entry);
        }

        private static class TypeDataCache<T> where T : unmanaged, IComponentData
        {
            public static TypeDataCacheEntry? Entry;
        }

        private sealed class ManagedTypeDataCacheEntry
        {
            public required EntityManager Owner;
            public required IManagedComponentTypeData Data;

            public ManagedTypeDataCacheEntry(EntityManager owner)
            {
                Owner = owner;
                owner.Register(this);
            }
        }

        private List<ManagedTypeDataCacheEntry> registeredCacheEntries = new();
        
        private void Register(ManagedTypeDataCacheEntry entry)
        {
            registeredCacheEntries.Add(entry);
        }

        private static class ManagedTypeDataCache<T> where T : class, IManagedComponentData
        {
            public static ManagedTypeDataCacheEntry? Entry;
        }

        public IComponentTypeData TypeData<T>() where T : unmanaged, IComponentData
        {
            var entry = TypeDataCache<T>.Entry;
            if (entry != null && ReferenceEquals(entry.Owner, this))
                return entry.Data;
            var data = TypeData(typeof(T));
            TypeDataCache<T>.Entry = new TypeDataCacheEntry(this) { Owner = this, Data = data };
            return data;
        }
        
        public IComponentTypeData TypeData(System.Type t)
        {
            if (typeToTypeDataMapping.TryGetValue(t, out var typeData))
                return typeData;
            if (!typeToIndexMapping.TryGetValue(t, out var index))
                index = typeToIndexMapping[t] = typeToIndexMapping.Count;
            if (index >= 32)
                throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            return typeToTypeDataMapping[t] = (IComponentTypeData)Activator.CreateInstance(typeof(ComponentTypeData<>).MakeGenericType(t), index)!;
        }

        internal int GetTypeIndex(System.Type t)
        {
            if (!typeToIndexMapping.TryGetValue(t, out var index))
            {
                index = typeToIndexMapping[t] = typeToIndexMapping.Count;
                if (index >= 32)
                    throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            }
            return index;
        }

        internal void RegisterTypeData(System.Type t, IComponentTypeData typeData)
        {
            typeToTypeDataMapping[t] = typeData;
        }

        public IManagedComponentTypeData ManagedTypeData<T>() where T : class, IManagedComponentData
        {
            var entry = ManagedTypeDataCache<T>.Entry;
            if (entry != null && ReferenceEquals(entry.Owner, this))
                return entry.Data;
            var data = ManagedTypeData(typeof(T));
            ManagedTypeDataCache<T>.Entry = new ManagedTypeDataCacheEntry(this) { Owner = this, Data = data };
            return data;
        }

        public IManagedComponentTypeData ManagedTypeData(System.Type t)
        {
            if (typeToManagedTypeDataMapping.TryGetValue(t, out var typeData))
                return typeData;
            if (!typeToManagedIndexMapping.TryGetValue(t, out var index))
                index = typeToManagedIndexMapping[t] = typeToManagedIndexMapping.Count;
            if (index >= 32)
                throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            typeData = (IManagedComponentTypeData)Activator.CreateInstance(typeof(ManagedComponentTypeData<>).MakeGenericType(t), index)!;
            typeToManagedTypeDataMapping[t] = typeData;
            return typeData;
        }

        public Archetype NewArchetype()
        {
            return new Archetype(this)
                .WithComponentData<EntityName>()
                .WithComponentData<Relationship>();
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
            entitiesChunk = null!;
            dataManager.Dispose();
            foreach (var registered in registeredCacheEntries)
            {
                registered.Owner = null!;
            }

            foreach (var registered in registeredUnmanagedCacheEntries)
            {
                registered.Owner = null!;
            }
            registeredCacheEntries.Clear();
            registeredUnmanagedCacheEntries.Clear();
        }
        
        internal Archetype GetArchetypeByEntity(Entity entity)
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return archetypes[entitiesArchetype[entity.Id]];
        }
        
        internal ChunkDataManager GetEntityDataManagerByEntity(Entity entity)
        {
#if DEBUG
            VerifyEntity(entity);
#endif
            return entitiesChunk[entity.Id]!;
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