using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace TheEngine.ECS
{
    public class Archetype
    {
        private BitVector32 usedComponents = new BitVector32();
        private BitVector32 usedManagedComponents = new BitVector32();
        private readonly List<IComponentTypeData> components = new();
        public IList<IComponentTypeData> Components => components;
        private readonly List<IManagedComponentTypeData> managedComponents = new();
        public IList<IManagedComponentTypeData> ManagedComponents => managedComponents;
        public IEntityManager EntityManager { get; }
        public uint ComponentBitMask => (uint)usedComponents.Data;
        public uint ManagedComponentBitMask => (uint)usedManagedComponents.Data;

        public ulong Hash => ComponentBitMask | (ulong)ManagedComponentBitMask << 32;
        
        internal Archetype(IEntityManager entityManager)
        {
            EntityManager = entityManager;
        }
        
        public Archetype WithManagedComponentData<T>() where T : class, IManagedComponentData
        {
            var n = new Archetype(EntityManager);
            n.components.AddRange(components);
            n.usedComponents = usedComponents;
            
            n.managedComponents.AddRange(managedComponents);
            var typeData = EntityManager.ManagedTypeData<T>();
            n.managedComponents.Add(typeData);
            n.usedManagedComponents = usedManagedComponents;
            n.usedManagedComponents[(int)typeData.Hash] = true;

            EntityManager.InstallArchetype(n);
            
            return n;
        }

        public Archetype WithComponentData<T>() where T : unmanaged, IComponentData
        {
            return WithComponentData(typeof(T));
        }
        
        internal Archetype WithComponentData(System.Type t)
        {
            var n = new Archetype(EntityManager);
            n.managedComponents.AddRange(managedComponents);
            n.usedManagedComponents = usedManagedComponents;

            n.components.AddRange(components);
            var typeData = EntityManager.TypeData(t);
            n.components.Add(typeData);
            n.usedComponents = usedComponents;
            n.usedComponents[(int)typeData.Hash] = true;
            
            EntityManager.InstallArchetype(n);
            
            return n;
        }

        public Archetype Includes(Archetype other)
        {
            var n = new Archetype(EntityManager);
            n.managedComponents.AddRange(managedComponents);
            n.usedManagedComponents = usedManagedComponents;
            n.components.AddRange(components);
            n.usedComponents = usedComponents;
            
            foreach (var component in other.Components)
            {
                if (!n.usedComponents[(int)component.Hash])
                {
                    n.components.Add(component);
                    n.usedComponents[(int)component.Hash] = true;   
                }
            }
            
            foreach (var component in other.ManagedComponents)
            {
                if (!n.usedManagedComponents[(int)component.Hash])
                {
                    n.managedComponents.Add(component);
                    n.usedManagedComponents[(int)component.Hash] = true;   
                }
            }
            
            EntityManager.InstallArchetype(n);
            return n;
        }

        public bool Contains(Archetype other)
        {
            return (usedComponents.Data & other.usedComponents.Data) == other.usedComponents.Data &&
                   (usedManagedComponents.Data & other.usedManagedComponents.Data) == other.usedManagedComponents.Data;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var comp in components)
                sb.Append(comp.DataType.Name + " + ");
            foreach (var comp in managedComponents)
                sb.Append(comp.DataType.Name + " + ");
            return sb.ToString();
        }
    }
}