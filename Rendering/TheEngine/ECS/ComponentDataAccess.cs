using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TheEngine.ECS
{
    public readonly struct ComponentDataAccess<T> where T : unmanaged, IComponentData
    {
        private readonly unsafe byte* data;
        private readonly int[] sparseReverseEntityMapping;

        public unsafe ComponentDataAccess(byte* data, int[] sparseReverseEntityMapping)
        {
            this.data = data;
            this.sparseReverseEntityMapping = sparseReverseEntityMapping;
        }

        public unsafe bool IsInitialized => data != (byte*)0;
        
        public bool Has(Entity e) => e.Id < sparseReverseEntityMapping.Length && sparseReverseEntityMapping[e.Id] != 0;
        
        public unsafe ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                //fixed (byte* p = data)
                return ref *(T*)(data + index * sizeof(T));
                //return ref Unsafe.AsRef<T>(p + index * sizeof(T));
            }
        }

        public unsafe ref T this[Entity index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var indexInArray = sparseReverseEntityMapping[index.Id] - 1;
                //fixed(byte* p = data)
                return ref Unsafe.AsRef<T>(data + indexInArray * sizeof(T));
            }
        }
    }
    
    public sealed class ManagedComponentDataAccess<T> where T : IManagedComponentData
    {
        private readonly object?[] data;
        private readonly int[] sparseReverseEntityMapping;

        public unsafe ManagedComponentDataAccess(object?[] data, int[] sparseReverseEntityMapping)
        {
            this.data = data;
            this.sparseReverseEntityMapping = sparseReverseEntityMapping;
        }
        
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (T)data[index]!;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => data[index] = value;
        }

        public T this[Entity index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var indexInArray = sparseReverseEntityMapping[index.Id] - 1;
                return (T)data[indexInArray]!;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                var indexInArray = sparseReverseEntityMapping[index.Id] - 1;
                data[indexInArray] = value;
            }
        }
    }
}