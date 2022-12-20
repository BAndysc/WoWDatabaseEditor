using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TheEngine.ECS
{
    internal class ChunkDataManager : IChunkDataIterator, System.IDisposable
    {
        private bool disposed;
        //private readonly byte[][] componentData;
        private unsafe byte*[] componentData;
        private object?[][] managedComponentData;
        public Archetype Archetype;
        private int capacity;
        private int used;
        private readonly int componentsCount;
        private readonly int managedComponentsCount;

        private Entity[] entityMapping;
        private int[] sparseReverseEntityMapping = new int[1];

        public unsafe ChunkDataManager(Archetype archetype)
        {
            Archetype = archetype;
            componentsCount = archetype.Components.Count;
            managedComponentsCount = archetype.ManagedComponents.Count;
            //componentData = new byte[archetype.Components.Count][];
            componentData = new byte*[componentsCount];
            managedComponentData = new object?[managedComponentsCount][];
        }

        public unsafe void Dispose()
        {
            disposed = true;
            Archetype = null!;
            for (int i = 0; i < componentsCount; ++i)
            {
                Marshal.FreeHGlobal(new IntPtr(componentData[i]));
                componentData[i] = null!;
            }
            for (int i = 0; i < managedComponentsCount; ++i)
            {
                managedComponentData[i] = null!;
            }
            componentData = null!;
            managedComponentData = null!;
            sparseReverseEntityMapping = null!;
        }

        ~ChunkDataManager()
        {
            if (!disposed)
            {
                Console.WriteLine("ChunkDataManger not disposed");
                Dispose();
            }
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => used;
        }

        public Entity this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entityMapping[index];
        }

        public ManagedComponentDataAccess<T>? OptionalManagedDataAccess<T>() where T : IManagedComponentData
        {
            int i = 0;
            // for to prevent allocations
            for (var index = 0; index < Archetype.ManagedComponents.Count; index++)
            {
                var c = Archetype.ManagedComponents[index];
                if (c.DataType == typeof(T))
                    return new ManagedComponentDataAccess<T>(managedComponentData[i], sparseReverseEntityMapping);
                i++;
            }

            return null;
        }
        
        public unsafe ComponentDataAccess<T>? OptionalDataAccess<T>() where T : unmanaged, IComponentData
        {
            int i = 0;
            var componentsCount = Archetype.Components.Count;
            for (int j = 0; j < componentsCount; ++j)
            {
                var c = Archetype.Components[j];
                if (c.DataType == typeof(T))
                    return new ComponentDataAccess<T>(componentData[i], sparseReverseEntityMapping);
                i++;
            }

            return null;
        }
        
        public ManagedComponentDataAccess<T> ManagedDataAccess<T>() where T : IManagedComponentData
        {
            return OptionalManagedDataAccess<T>() ?? throw new Exception("There is no component data + " + typeof(T) + " in this archetype");
        }
        
        public ComponentDataAccess<T> DataAccess<T>() where T : unmanaged, IComponentData
        {
            return OptionalDataAccess<T>() ?? throw new Exception("There is no component data + " + typeof(T) + " in this archetype");
        }

        private static unsafe void AllocOrRealloc(ref byte* addr, ulong size)
        {
            if (addr == null)
            {
                addr = (byte*)Marshal.AllocHGlobal((IntPtr)size).ToPointer();
            }
            else
            {
                addr = (byte*)Marshal.ReAllocHGlobal((IntPtr)addr, (IntPtr)size).ToPointer();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ResizeIfNeeded(Entity entity)
        {
            if (capacity <= used)
            {
                capacity = capacity * 2 + 1;
                Array.Resize(ref entityMapping, capacity);
                for (int i = 0; i < componentsCount; ++i)
                {
                    AllocOrRealloc(ref componentData[i], (ulong)capacity * (ulong)Archetype.Components[i].SizeBytes);
                    //Array.Resize(ref componentData[i], capacity * Archetype.Components[i].SizeBytes);
                }
                for (int i = 0; i < managedComponentsCount; ++i)
                {
                    Array.Resize(ref managedComponentData[i], capacity);
                }
            }
            if (sparseReverseEntityMapping.Length <= entity.Id)
                Array.Resize(ref sparseReverseEntityMapping, Math.Max((int)entity.Id + 1, sparseReverseEntityMapping.Length * 2 + 1));
        }
        
        /**
         * The entity must be present in the Data Manager!
         */
        internal unsafe void UnsafeCopy(Entity entity, ChunkDataManager oldData, IComponentTypeData component)
        {
            var newIndex = Archetype.Components.IndexOf(component);
            var oldIndex = oldData.Archetype.Components.IndexOf(component);
            
            if (newIndex == -1 || oldIndex == -1)
                throw new Exception("trying to unsafe copy component which is not present either in the old or new ComponentDataManager");
            
            var newArray = componentData[newIndex];
            var oldArray = oldData.componentData[oldIndex];

            var newEntityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            var oldEntityIndex = oldData.sparseReverseEntityMapping[entity.Id] - 1;

            for (int j = 0; j < component.SizeBytes; ++j)
                newArray[newEntityIndex * component.SizeBytes + j] = oldArray[oldEntityIndex * component.SizeBytes + j];
        }

        internal void UnsafeCopy(Entity entity, ChunkDataManager oldData, IManagedComponentTypeData component)
        {
            var newIndex = Archetype.ManagedComponents.IndexOf(component);
            var oldIndex = oldData.Archetype.ManagedComponents.IndexOf(component);
            
            if (newIndex == -1 || oldIndex == -1)
                throw new Exception("trying to unsafe copy component which is not present either in the old or new ComponentDataManager");
            
            var newArray = managedComponentData[newIndex];
            var oldArray = oldData.managedComponentData[oldIndex];

            var newEntityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            var oldEntityIndex = oldData.sparseReverseEntityMapping[entity.Id] - 1;

            newArray[newEntityIndex] = oldArray[oldEntityIndex];
        }
        
        public unsafe void AddEntity(Entity entity)
        {
            ResizeIfNeeded(entity);
            entityMapping[used] = entity;
            sparseReverseEntityMapping[entity.Id] = used + 1;
            for (var index = 0; index < componentsCount; index++)
            {
                var c = Archetype.Components[index];
                var array = componentData[index];
                // zero array
                for (int j = 0; j < c.SizeBytes; ++j)
                    array[used * c.SizeBytes + j] = 0;
            }
            for (var index = 0; index < managedComponentsCount; index++)
            {
                var c = Archetype.ManagedComponents[index];
                var array = managedComponentData[index];
                array[used] = null;
            }

            used++;
        }

        public unsafe void RemoveEntity(Entity entity)
        {
            var index = sparseReverseEntityMapping[entity.Id] - 1;
            var swapWith = used - 1;
            var swapWithEntity = entityMapping[swapWith];

            entityMapping[index] = swapWithEntity;
            sparseReverseEntityMapping[swapWithEntity.Id] = index + 1;
            sparseReverseEntityMapping[entity.Id] = 0;

            int i = 0;
            foreach (var c in Archetype.Components)
            {
                var array = componentData[i];
                for (int j = 0; j < c.SizeBytes; ++j)
                    array[index * c.SizeBytes + j] = array[swapWith * c.SizeBytes + j];
                i++;
            }
            
            i = 0;
            foreach (var c in Archetype.ManagedComponents)
            {
                var array = managedComponentData[i];
                array[index] = array[swapWith];
                i++;
            }
            
            used--;
        }

        // for debugging only
        internal object? DebugGetManagedComponent(Entity entity, IManagedComponentTypeData type)
        {
            for (int j = 0; j < managedComponentsCount; ++j)
            {
                var c = Archetype.ManagedComponents[j];
                if (c.DataType == type.DataType)
                {
                    var index = sparseReverseEntityMapping[entity.Id];
                    return managedComponentData[j][index];
                }
            }

            return null;
        }
        
        internal unsafe object? UnsafeDebugGetComponent(Entity entity, IComponentTypeData type)
        {
            for (int j = 0; j < componentsCount; ++j)
            {
                var c = Archetype.Components[j];
                if (c.DataType == type.DataType)
                {
                    var index = sparseReverseEntityMapping[entity.Id];
                    return Marshal.PtrToStructure(new IntPtr(componentData[j] + index * c.SizeBytes), c.DataType);
                }
            }

            return null;
        }
    }
}