using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;

namespace TheEngine.ECS
{
    public interface IComponentTypeData<T> : IComponentTypeData where T : unmanaged, IComponentData
    {}

    internal class EntityManager : IEntityManager, System.IDisposable
    {
        private readonly EntityDataManager dataManager = new();
        private readonly List<Entity> freeEntities = new();
        private Entity[] entities = new Entity[1];
        private int[] entitiesArchetype = new int[1];
        private uint used;
        private readonly Dictionary<System.Type, int> typeToIndexMapping = new();

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
            entitiesArchetype[newEntity.Id] = archetype.ComponentBitMask;
            dataManager.AddEntity(newEntity, archetype);
            return newEntity;
        }

        public void DestroyEntity(Entity entity)
        {
            dataManager.RemoveEntity(entity, entitiesArchetype[entity.Id]);
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
                index = typeToIndexMapping[typeof(T)] = typeToIndexMapping.Count + 1;
            if (index >= 32)
                throw new Exception("Currently there is limit of 32 different component datas. If you need more, change BitVector32 to BitVector64 or BitArray");
            return new ComponentTypeData<T>(index);
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