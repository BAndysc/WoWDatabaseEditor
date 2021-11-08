using System.Collections.Generic;
using System.Collections.Specialized;

namespace TheEngine.ECS
{
    public class Archetype
    {
        private BitVector32 usedComponents = new BitVector32();
        private readonly List<IComponentTypeData> components = new();
        public IList<IComponentTypeData> Components => components;
        public IEntityManager EntityManager { get; }
        public int ComponentBitMask => usedComponents.Data;

        internal Archetype(IEntityManager entityManager)
        {
            EntityManager = entityManager;
        }
        
        public Archetype WithComponentData<T>() where T : unmanaged, IComponentData
        {
            var n = new Archetype(EntityManager);
            n.components.AddRange(components);
            var typeData = EntityManager.TypeData<T>();
            n.components.Add(typeData);
            n.usedComponents = usedComponents;
            n.usedComponents[(1 << typeData.Index)] = true;
            return n;
        }

        public bool Contains(Archetype other)
        {
            return (usedComponents.Data & other.usedComponents.Data) == other.usedComponents.Data;
        }
    }
}