using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TheEngine.Interfaces;
using TheEngine.Managers;

namespace TheEngine.ECS
{
    internal struct ComponentArrayIndex
    {
        public int Index;
        public int Count;
        public int Capacity; // power-of-two block size in elements, 0 = no block allocated
    }

    internal class ChunkDataManager : IChunkDataIterator, System.IDisposable
    {
        private bool disposed;
        private unsafe byte*[] componentData;
        private unsafe byte*[] arrayComponentData; // Backing buffers for array components, managed by a buddy allocator
        private int[] arrayComponentCapacities; // Total capacities (in elements, always a power of two) for array components
        private HashSet<int>[]?[] arrayFreeBlocks; // Per array component: free block element offsets, indexed by size class (log2 of block size)
        private object?[][] managedComponentData;
        public Archetype Archetype;
        private readonly StatsManager statsManager;
        private readonly Engine engine;
        private readonly EntityManager entityManager;
        private int capacity;
        private int used;
        private readonly int componentsCount;
        private readonly int managedComponentsCount;

        // global component-type index -> slot in this archetype (-1 = not present). Turns the
        // per-access "linear scan comparing System.Type" into a single array read; both index
        // spaces are hard-capped at 32 by EntityManager.
        private readonly int[] slotByTypeIndex = new int[32];
        private readonly int[] managedSlotByTypeIndex = new int[32];

        private Entity[] entityMapping;
        private int[] sparseReverseEntityMapping = new int[1];

        public unsafe ChunkDataManager(Archetype archetype, StatsManager statsManager, Engine engine)
        {
            Archetype = archetype;
            this.statsManager = statsManager;
            this.engine = engine;
            entityManager = (EntityManager)archetype.EntityManager;
            componentsCount = archetype.Components.Count;
            managedComponentsCount = archetype.ManagedComponents.Count;
            componentData = new byte*[componentsCount];
            arrayComponentData = new byte*[componentsCount];
            arrayComponentCapacities = new int[componentsCount];
            arrayFreeBlocks = new HashSet<int>[componentsCount][];
            managedComponentData = new object?[managedComponentsCount][];

            Array.Fill(slotByTypeIndex, -1);
            Array.Fill(managedSlotByTypeIndex, -1);
            for (int i = 0; i < componentsCount; i++)
                slotByTypeIndex[archetype.Components[i].Index] = i;
            for (int i = 0; i < managedComponentsCount; i++)
                managedSlotByTypeIndex[archetype.ManagedComponents[i].Index] = i;

            // Initialize array component storage
            for (int i = 0; i < componentsCount; i++)
            {
                if (archetype.Components[i].IsArray)
                {
                    arrayComponentCapacities[i] = InitialArrayCapacity;
                    arrayComponentData[i] = (byte*)Marshal.AllocHGlobal(InitialArrayCapacity * archetype.Components[i].SizeBytes);
                    statsManager.EntitiesUnmanagedBytes += (ulong)InitialArrayCapacity * (ulong)archetype.Components[i].SizeBytes;
                    var maxClass = BitOperations.Log2((uint)InitialArrayCapacity);
                    var freeLists = new HashSet<int>[maxClass + 1];
                    for (int cls = 0; cls <= maxClass; cls++)
                        freeLists[cls] = new HashSet<int>();
                    freeLists[maxClass].Add(0); // the whole buffer starts as one free block
                    arrayFreeBlocks[i] = freeLists;
                }
            }
        }

        public unsafe void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            // Fire OnRemoved hooks for every still-live entity, just like RemoveEntity(entity, isDestroyed: true)
            // would. Freeing the backing memory without this leaks anything the hooks release.
            for (int i = 0; i < componentsCount; ++i)
            {
                var comp = Archetype.Components[i];
                if (comp.OnRemovedAction == null)
                    continue;

                if (comp.IsArray)
                {
                    var arrayIndex = (ComponentArrayIndex*)componentData[i];
                    for (int e = 0; e < used; ++e)
                    {
                        var entityArrayInfo = arrayIndex[e];
                        for (int k = 0; k < entityArrayInfo.Count; k++)
                        {
                            var componentPtr = arrayComponentData[i] + (entityArrayInfo.Index + k) * comp.SizeBytes;
                            comp.OnRemovedAction(engine, entityMapping[e], new Span<byte>(componentPtr, comp.SizeBytes));
                        }
                    }
                }
                else
                {
                    var array = componentData[i];
                    for (int e = 0; e < used; ++e)
                        comp.OnRemovedAction(engine, entityMapping[e], new Span<byte>(array + e * comp.SizeBytes, comp.SizeBytes));
                }
            }

            // give the stats back before the capacities are lost, so EntitiesUnmanagedBytes doesn't
            // drift upwards with every destroyed archetype
            for (int i = 0; i < componentsCount; ++i)
            {
                var component = Archetype.Components[i];
                var sizeToUse = component.IsArray ? sizeof(ComponentArrayIndex) : component.SizeBytes;
                if (componentData[i] != null)
                    statsManager.EntitiesUnmanagedBytes -= (ulong)capacity * (ulong)sizeToUse;
                if (arrayComponentData[i] != null)
                    statsManager.EntitiesUnmanagedBytes -= (ulong)arrayComponentCapacities[i] * (ulong)component.SizeBytes;
            }

            Archetype = null!;
            FreeNativeBlocks();
            for (int i = 0; i < managedComponentsCount; ++i)
            {
                managedComponentData[i] = null!;
            }
            componentData = null!;
            arrayComponentData = null!;
            arrayComponentCapacities = null!;
            arrayFreeBlocks = null!;
            managedComponentData = null!;
            sparseReverseEntityMapping = null!;
            GC.SuppressFinalize(this);
        }

        private unsafe void FreeNativeBlocks()
        {
            for (int i = 0; i < componentsCount; ++i)
            {
                if (componentData[i] != null)
                {
                    Marshal.FreeHGlobal(new IntPtr(componentData[i]));
                    componentData[i] = null!;
                }
                if (arrayComponentData[i] != null)
                {
                    Marshal.FreeHGlobal(new IntPtr(arrayComponentData[i]));
                    arrayComponentData[i] = null!;
                }
            }
        }

        ~ChunkDataManager()
        {
            if (!disposed)
            {
                // finalizer thread: only reclaim the native blocks, running the OnRemoved hooks
                // (which touch the engine) is not safe here
                Console.WriteLine("ChunkDataManger not disposed");
                FreeNativeBlocks();
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
            var slot = managedSlotByTypeIndex[entityManager.ManagedTypeData(typeof(T)).Index];
            if (slot < 0)
                return null;
            return new ManagedComponentDataAccess<T>(managedComponentData[slot], sparseReverseEntityMapping);
        }

        public ManagedComponentDataAccess<T> ManagedDataAccess<T>() where T : IManagedComponentData
        {
            return OptionalManagedDataAccess<T>() ?? throw new Exception("There is no managed component data + " + typeof(T) + " in this archetype");
        }

        public unsafe ComponentDataAccess<T>? OptionalDataAccess<T>() where T : unmanaged, IComponentData
        {
            var typeData = entityManager.TypeData<T>();
            var slot = slotByTypeIndex[typeData.Index];
            if (slot < 0)
                return null;
            if (typeData.IsArray)
                throw new Exception("Component " + typeof(T) + " is an array component, use OptionalArrayDataAccess instead");
            return new ComponentDataAccess<T>(componentData[slot], sparseReverseEntityMapping);
        }

        public unsafe ComponentArrayDataAccess<T>? OptionalArrayDataAccess<T>() where T : unmanaged, IComponentData
        {
            var typeData = entityManager.TypeData<T>();
            var slot = slotByTypeIndex[typeData.Index];
            if (slot < 0 || !typeData.IsArray)
                return null;
            return new ComponentArrayDataAccess<T>(componentData[slot], arrayComponentData[slot], sparseReverseEntityMapping);
        }
        
        public ComponentDataAccess<T> DataAccess<T>() where T : unmanaged, IComponentData
        {
            return OptionalDataAccess<T>() ?? throw new Exception("There is no component data + " + typeof(T) + " in this archetype");
        }

        public ComponentArrayDataAccess<T> ArrayDataAccess<T>() where T : unmanaged, IComponentData
        {
            return OptionalArrayDataAccess<T>() ?? throw new Exception("There is no array component data + " + typeof(T) + " in this archetype");
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

        private const int InitialArrayCapacity = 16; // elements, must be a power of two
        private const int MinBlockSizeClass = 1; // the smallest per-entity block is 2 elements

        // Entity blocks are sized to the next power of two >= count, but never smaller than the minimum block.
        private static int SizeClassForCount(int count)
        {
            return Math.Max(MinBlockSizeClass, BitOperations.Log2(BitOperations.RoundUpToPowerOf2((uint)count)));
        }

        // Buddy allocator: allocates a block of 2^sizeClass elements from the component's backing buffer
        // and returns its element offset. May grow (and therefore realloc) the backing buffer, so any
        // pointer into arrayComponentData[componentIndex] must be (re)fetched after calling this.
        private int AllocBlock(int componentIndex, int sizeClass)
        {
            while (true)
            {
                var freeLists = arrayFreeBlocks[componentIndex]!;
                for (int cls = sizeClass; cls < freeLists.Length; cls++)
                {
                    if (freeLists[cls].Count == 0)
                        continue;

                    int offset = 0;
                    foreach (var free in freeLists[cls])
                    {
                        offset = free;
                        break;
                    }
                    freeLists[cls].Remove(offset);

                    // split down to the requested size, the upper halves become free blocks
                    while (cls > sizeClass)
                    {
                        cls--;
                        freeLists[cls].Add(offset + (1 << cls));
                    }
                    return offset;
                }
                GrowArrayComponentBuffer(componentIndex);
            }
        }

        // Returns a block to the free lists, merging it with its buddy (offset XOR size) as long as possible.
        private void FreeBlock(int componentIndex, int offset, int sizeClass)
        {
            var freeLists = arrayFreeBlocks[componentIndex]!;
            while (sizeClass < freeLists.Length - 1)
            {
                var buddy = offset ^ (1 << sizeClass);
                if (!freeLists[sizeClass].Remove(buddy))
                    break;
                offset = Math.Min(offset, buddy);
                sizeClass++;
            }
            freeLists[sizeClass].Add(offset);
        }

        private unsafe void GrowArrayComponentBuffer(int componentIndex)
        {
            var component = Archetype.Components[componentIndex];
            var oldCapacity = arrayComponentCapacities[componentIndex];
            var newCapacity = oldCapacity * 2;
            statsManager.EntitiesUnmanagedBytes += (ulong)oldCapacity * (ulong)component.SizeBytes;
            AllocOrRealloc(ref arrayComponentData[componentIndex], (ulong)newCapacity * (ulong)component.SizeBytes);
            arrayComponentCapacities[componentIndex] = newCapacity;

            var freeLists = arrayFreeBlocks[componentIndex]!;
            Array.Resize(ref freeLists, freeLists.Length + 1);
            freeLists[^1] = new HashSet<int>();
            arrayFreeBlocks[componentIndex] = freeLists;

            // the new upper half enters the allocator as one free block (it may merge with the old top)
            FreeBlock(componentIndex, oldCapacity, BitOperations.Log2((uint)oldCapacity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ResizeIfNeeded(Entity entity)
        {
            if (capacity <= used)
            {
                var oldCapacity = capacity;
                capacity = capacity * 2 + 1;
                Array.Resize(ref entityMapping, capacity);
                for (int i = 0; i < componentsCount; ++i)
                {
                    var component = Archetype.Components[i];
                    var sizeToUse = component.IsArray ? sizeof(ComponentArrayIndex) : component.SizeBytes;
                    var oldSize = (ulong)oldCapacity * (ulong)sizeToUse;
                    var newSize = (ulong)capacity * (ulong)sizeToUse;
                    var delta = newSize - oldSize;
                    statsManager.EntitiesUnmanagedBytes += delta;
                    AllocOrRealloc(ref componentData[i], newSize);
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
            
            var newEntityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            var oldEntityIndex = oldData.sparseReverseEntityMapping[entity.Id] - 1;

            if (component.IsArray)
            {
                // Copy array component data into a freshly allocated block in this chunk.
                // The old chunk's block is released when the entity is removed from it (RemoveEntity).
                var oldArrayIndex = (ComponentArrayIndex*)oldData.componentData[oldIndex];
                var newArrayIndex = (ComponentArrayIndex*)componentData[newIndex];
                var oldIndexStruct = oldArrayIndex[oldEntityIndex];

                if (oldIndexStruct.Count > 0)
                {
                    var sizeClass = SizeClassForCount(oldIndexStruct.Count);
                    var newOffset = AllocBlock(newIndex, sizeClass);

                    // fetch pointers after AllocBlock, it may realloc the backing buffer
                    var sourcePtr = oldData.arrayComponentData[oldIndex] + (long)oldIndexStruct.Index * component.SizeBytes;
                    var destPtr = arrayComponentData[newIndex] + (long)newOffset * component.SizeBytes;
                    Buffer.MemoryCopy(sourcePtr, destPtr, (long)(1 << sizeClass) * component.SizeBytes, (long)oldIndexStruct.Count * component.SizeBytes);

                    newArrayIndex[newEntityIndex] = new ComponentArrayIndex
                    {
                        Index = newOffset,
                        Count = oldIndexStruct.Count,
                        Capacity = 1 << sizeClass
                    };
                }
                else
                {
                    // No components to copy
                    newArrayIndex[newEntityIndex] = default;
                }
            }
            else
            {
                // Copy regular component data
                var newArray = componentData[newIndex];
                var oldArray = oldData.componentData[oldIndex];

                for (int j = 0; j < component.SizeBytes; ++j)
                    newArray[newEntityIndex * component.SizeBytes + j] = oldArray[oldEntityIndex * component.SizeBytes + j];
            }
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
        
        // invokeOnAddedHooks is false during archetype moves, to avoid re-firing OnAdded for existing components
        public unsafe void AddEntity(Entity entity, bool invokeOnAddedHooks)
        {
            ResizeIfNeeded(entity);
            entityMapping[used] = entity;
            sparseReverseEntityMapping[entity.Id] = used + 1;
            for (var index = 0; index < componentsCount; index++)
            {
                var comp = Archetype.Components[index];
                if (comp.IsArray)
                {
                    // Initialize array index structure
                    var arrayIndex = (ComponentArrayIndex*)componentData[index];
                    arrayIndex[used] = default;
                }
                else
                {
                    // Zero regular component data
                    var array = componentData[index];
                    for (int j = 0; j < comp.SizeBytes; ++j)
                        array[used * comp.SizeBytes + j] = 0;

                    if (invokeOnAddedHooks && comp.OnAddedAction != null)
                        comp.OnAddedAction(engine, entity, new Span<byte>(array + used * comp.SizeBytes, comp.SizeBytes));
                }
            }
            for (var index = 0; index < managedComponentsCount; index++)
            {
                var array = managedComponentData[index];
                array[used] = null;
            }

            used++;
        }

        public unsafe void RemoveEntity(Entity entity, bool isDestroyed)
        {
            var index = sparseReverseEntityMapping[entity.Id] - 1;
            var swapWith = used - 1;
            var swapWithEntity = entityMapping[swapWith];

            entityMapping[index] = swapWithEntity;
            sparseReverseEntityMapping[swapWithEntity.Id] = index + 1;
            sparseReverseEntityMapping[entity.Id] = 0;

            // Handle component data copying
            for (int i = 0; i < componentsCount; i++)
            {
                var comp = Archetype.Components[i];

                if (comp.IsArray)
                {
                    var arrayIndex = (ComponentArrayIndex*)componentData[i];
                    var entityArrayInfo = arrayIndex[index];
                    var swapWithArrayInfo = arrayIndex[swapWith];

                    if (isDestroyed && comp.OnRemovedAction != null && entityArrayInfo.Count > 0)
                    {
                        // Free all components for this entity
                        for (int k = 0; k < entityArrayInfo.Count; k++)
                        {
                            var componentPtr = arrayComponentData[i] + (entityArrayInfo.Index + k) * comp.SizeBytes;
                            comp.OnRemovedAction(engine, entity, new Span<byte>(componentPtr, comp.SizeBytes));
                        }
                    }

                    // Release the entity's block regardless of isDestroyed - on archetype moves
                    // the data was already copied to the new chunk by UnsafeCopy.
                    if (entityArrayInfo.Capacity > 0)
                        FreeBlock(i, entityArrayInfo.Index, BitOperations.Log2((uint)entityArrayInfo.Capacity));

                    // The swapped entity keeps its block, only the index slot moves
                    arrayIndex[index] = swapWithArrayInfo;
                    arrayIndex[swapWith] = default;
                }
                else
                {
                    // Handle regular components
                    var array = componentData[i];
                    if (isDestroyed && comp.OnRemovedAction != null)
                    {
                        comp.OnRemovedAction(engine, entity, new Span<byte>(array + index * comp.SizeBytes, comp.SizeBytes));
                    }
                    // Copy component data from swapWith entity to removed entity's position
                    for (int j = 0; j < comp.SizeBytes; ++j)
                        array[index * comp.SizeBytes + j] = array[swapWith * comp.SizeBytes + j];
                }
            }
            
            // Handle managed components
            for (int i = 0; i < managedComponentsCount; i++)
            {
                var array = managedComponentData[i];
                array[index] = array[swapWith];
                array[swapWith] = null; // free the reference to let GC collect it
            }
            
            used--;
        }

        public unsafe void AddArrayComponent<T>(Entity entity, T component) where T : unmanaged, IComponentData
        {
            var typeData = entityManager.TypeData<T>();
            int componentIndex = typeData.IsArray ? slotByTypeIndex[typeData.Index] : -1;

            if (componentIndex == -1)
                throw new Exception($"Component {typeof(T)} is not an array component in this archetype");

            var entityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            var arrayIndex = (ComponentArrayIndex*)componentData[componentIndex];
            var currentInfo = arrayIndex[entityIndex];

            if (currentInfo.Count == currentInfo.Capacity)
            {
                // Block is full (or not allocated yet), move to a bigger one
                var sizeClass = SizeClassForCount(currentInfo.Count + 1);
                var newOffset = AllocBlock(componentIndex, sizeClass);

                // fetch the pointer after AllocBlock, it may realloc the backing buffer
                var data = arrayComponentData[componentIndex];
                if (currentInfo.Count > 0)
                {
                    Buffer.MemoryCopy(
                        data + (long)currentInfo.Index * sizeof(T),
                        data + (long)newOffset * sizeof(T),
                        (long)(1 << sizeClass) * sizeof(T),
                        (long)currentInfo.Count * sizeof(T));
                    FreeBlock(componentIndex, currentInfo.Index, BitOperations.Log2((uint)currentInfo.Capacity));
                }
                currentInfo.Index = newOffset;
                currentInfo.Capacity = 1 << sizeClass;
            }

            // Write the new component
            var componentPtr = (T*)(arrayComponentData[componentIndex] + (long)(currentInfo.Index + currentInfo.Count) * sizeof(T));
            *componentPtr = component;
            currentInfo.Count++;

            // Update the index structure
            arrayIndex[entityIndex] = currentInfo;

            // every call here is a genuinely new array element, so OnAdded always fires
            Archetype.Components[componentIndex].OnAddedAction?.Invoke(engine, entity, new Span<byte>(componentPtr, sizeof(T)));
        }

        public unsafe bool RemoveArrayComponent<T>(Entity entity, int componentIndex = 0) where T : unmanaged, IComponentData
        {
            var typeData = entityManager.TypeData<T>();
            int arrayComponentIndex = typeData.IsArray ? slotByTypeIndex[typeData.Index] : -1;

            if (arrayComponentIndex == -1)
                return false;

            var entityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            var arrayIndex = (ComponentArrayIndex*)componentData[arrayComponentIndex];
            var currentInfo = arrayIndex[entityIndex];

            if (currentInfo.Count == 0 || componentIndex >= currentInfo.Count)
                return false;

            var data = arrayComponentData[arrayComponentIndex];
            var removeAt = currentInfo.Index + componentIndex;

            // Free the component if needed
            var componentTypeData = Archetype.Components[arrayComponentIndex];
            if (componentTypeData.OnRemovedAction != null)
            {
                var componentPtr = data + (long)removeAt * sizeof(T);
                componentTypeData.OnRemovedAction(engine, entity, new Span<byte>(componentPtr, sizeof(T)));
            }

            // Close the gap within the entity's own block, other entities are unaffected
            var moveCount = currentInfo.Count - componentIndex - 1;
            if (moveCount > 0)
            {
                var source = new Span<T>(data + (long)(removeAt + 1) * sizeof(T), moveCount);
                var dest = new Span<T>(data + (long)removeAt * sizeof(T), moveCount);
                source.CopyTo(dest); // Span.CopyTo handles overlapping ranges
            }

            currentInfo.Count--;
            if (currentInfo.Count == 0)
            {
                FreeBlock(arrayComponentIndex, currentInfo.Index, BitOperations.Log2((uint)currentInfo.Capacity));
                currentInfo = default;
            }
            arrayIndex[entityIndex] = currentInfo;

            return true;
        }

        public unsafe Span<T> GetArrayComponents<T>(Entity entity) where T : unmanaged, IComponentData
        {
            var typeData = entityManager.TypeData<T>();
            int componentIndex = typeData.IsArray ? slotByTypeIndex[typeData.Index] : -1;

            if (componentIndex == -1)
                return Span<T>.Empty;

            var entityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            var arrayIndex = (ComponentArrayIndex*)componentData[componentIndex];
            var info = arrayIndex[entityIndex];

            if (info.Count == 0)
                return Span<T>.Empty;

            var ptr = (T*)(arrayComponentData[componentIndex] + info.Index * sizeof(T));
            return new Span<T>(ptr, info.Count);
        }

        // for debugging only
        internal object? DebugGetManagedComponent(Entity entity, IManagedComponentTypeData type)
        {
            for (int j = 0; j < managedComponentsCount; ++j)
            {
                var managedComp = Archetype.ManagedComponents[j];
                if (managedComp.DataType == type.DataType)
                {
                    var index = sparseReverseEntityMapping[entity.Id] - 1;
                    if (index >= managedComponentData[j].Length)
                        return null;
                    return managedComponentData[j][index];
                }
            }

            return null;
        }

        internal unsafe byte* UnsafeDebugGetComponent(Entity entity, IComponentTypeData type)
        {
            for (int j = 0; j < componentsCount; ++j)
            {
                var comp = Archetype.Components[j];
                if (comp.DataType == type.DataType)
                {
                    var index = sparseReverseEntityMapping[entity.Id];
                    if (comp.IsArray)
                    {
                        // For array components, return pointer to the ComponentArrayIndex structure
                        return componentData[j] + (index - 1) * sizeof(ComponentArrayIndex);
                    }
                    else
                    {
                        return componentData[j] + (index - 1) * comp.SizeBytes;
                    }
                }
            }

            return null;
        }

        internal unsafe byte* UnsafeDebugGetArrayBytesComponent(Entity entity, IComponentTypeData type)
        {
            for (int j = 0; j < componentsCount; ++j)
            {
                var comp = Archetype.Components[j];
                if (comp.DataType == type.DataType)
                {
                    var index = sparseReverseEntityMapping[entity.Id];
                    if (comp.IsArray)
                    {
                        // For array components, return pointer to the ComponentArrayIndex structure
                        return arrayComponentData[j];
                    }
                    else
                    {
                        throw new Exception("Trying to get array bytes for a non-array component: " + type.DataType);
                    }
                }
            }

            return null;
        }
    }
}
