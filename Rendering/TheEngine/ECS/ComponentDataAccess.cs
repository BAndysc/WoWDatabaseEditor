using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TheEngine.ECS
{
    public class ComponentDataAccess<T> where T : unmanaged, IComponentData
    {
        private readonly byte[] data;
        private readonly int[] sparseReverseEntityMapping;

        public ComponentDataAccess(byte[] data, int[] sparseReverseEntityMapping)
        {
            this.data = data;
            this.sparseReverseEntityMapping = sparseReverseEntityMapping;
        }
        
        public unsafe ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MemoryMarshal.AsRef<T>(data.AsSpan(index * sizeof(T), sizeof(T)));
        }

        public unsafe ref T this[Entity index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var indexInArray = sparseReverseEntityMapping[index.Id] - 1;
                fixed(byte* p = data)
                    return ref Unsafe.AsRef<T>(p + indexInArray * sizeof(T));
//                return ref MemoryMarshal.AsRef<T>(data.AsSpan(indexInArray * sizeof(T), sizeof(T)));
            }
        }
    }
}