using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TheEngine.ECS
{
    internal class ChunkDataManager : IChunkDataIterator, System.IDisposable
    {
        private bool disposed;
        //private readonly byte[][] componentData;
        private readonly unsafe byte*[] componentData;
        public readonly Archetype Archetype;
        private int capacity;
        private int used;
        private readonly int componentsCount;

        private Entity[] entityMapping;
        private int[] sparseReverseEntityMapping = new int[1];

        public unsafe ChunkDataManager(Archetype archetype)
        {
            Archetype = archetype;
            componentsCount = archetype.Components.Count;
            //componentData = new byte[archetype.Components.Count][];
            componentData = new byte*[archetype.Components.Count];
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

        public unsafe ComponentDataAccess<T> DataAccess<T>() where T : unmanaged, IComponentData
        {
            int i = 0;
            foreach (var c in Archetype.Components)
            {
                if (c.DataType == typeof(T))
                    return new ComponentDataAccess<T>(componentData[i], sparseReverseEntityMapping);
                i++;
            }
            throw new Exception("There is no component data + " + typeof(T) + " in this archetype");
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
                    var newMem = Marshal.ReAllocHGlobal((IntPtr)componentData[i],
                        (IntPtr)(capacity * Archetype.Components[i].SizeBytes));
                    componentData[i] = (byte*)newMem.ToPointer();
                    //Array.Resize(ref componentData[i], capacity * Archetype.Components[i].SizeBytes);
                }
            }
            if (sparseReverseEntityMapping.Length <= entity.Id)
                Array.Resize(ref sparseReverseEntityMapping, Math.Max((int)entity.Id + 1, sparseReverseEntityMapping.Length * 2 + 1));
        }

        public unsafe void AddEntity(Entity entity)
        {
            ResizeIfNeeded(entity);
            entityMapping[used] = entity;
            sparseReverseEntityMapping[entity.Id] = used + 1;
            for (var index = 0; index < Archetype.Components.Count; index++)
            {
                var c = Archetype.Components[index];
                var array = componentData[index];
                // zero array
                for (int j = 0; j < c.SizeBytes; ++j)
                    array[used * c.SizeBytes + j] = 0;
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

            int i = 0;
            foreach (var c in Archetype.Components)
            {
                var array = componentData[i];
                for (int j = 0; j < c.SizeBytes; ++j)
                    array[index * c.SizeBytes + j] = array[swapWith * c.SizeBytes + j];
                i++;
            }
            
            used--;
        }

        public unsafe void Dispose()
        {
            disposed = true;
            for (int i = 0; i < componentsCount; ++i)
            {
                Marshal.FreeHGlobal(new IntPtr(componentData[i]));
                componentData[i] = null!;
            }
            sparseReverseEntityMapping = null!;
        }
    }
}