using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TheEngine.ECS
{
    public readonly struct ComponentArrayDataAccess<T> where T : unmanaged, IComponentData
    {
        private readonly unsafe byte* indexData; // ComponentArrayIndex structures
        private readonly unsafe byte* arrayData; // Flat array of actual components
        private readonly int[] sparseReverseEntityMapping;

        public unsafe ComponentArrayDataAccess(byte* indexData, byte* arrayData, int[] sparseReverseEntityMapping)
        {
            this.indexData = indexData;
            this.arrayData = arrayData;
            this.sparseReverseEntityMapping = sparseReverseEntityMapping;
        }

        public unsafe bool IsInitialized => indexData != (byte*)0 && arrayData != (byte*)0;

        public bool Has(Entity e) => e.Id < sparseReverseEntityMapping.Length && sparseReverseEntityMapping[e.Id] != 0;

        // Get the number of components for a specific entity
        public unsafe int GetCount(Entity entity)
        {
            if (!Has(entity))
                return 0;
            var entityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            var arrayIndex = (ComponentArrayIndex*)(indexData + entityIndex * sizeof(ComponentArrayIndex));
            return arrayIndex->Count;
        }

        // Access a specific component by entity and component index
        public unsafe ref T this[Entity entity, int componentIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var entityIndex = sparseReverseEntityMapping[entity.Id] - 1;
                var arrayIndex = (ComponentArrayIndex*)(indexData + entityIndex * sizeof(ComponentArrayIndex));

                if (componentIndex >= arrayIndex->Count)
                    throw new IndexOutOfRangeException($"Component index {componentIndex} is out of range for entity {entity} (count: {arrayIndex->Count})");

                var actualIndex = arrayIndex->Index + componentIndex;
                return ref *(T*)(arrayData + actualIndex * sizeof(T));
            }
        }

        // Get a span of all components for a specific entity
        public unsafe Span<T> GetComponents(Entity entity)
        {
            if (!Has(entity))
                return Span<T>.Empty;
            var entityIndex = sparseReverseEntityMapping[entity.Id] - 1;
            return this[entityIndex];
        }

        // Direct access to flat array by absolute index (for systems that iterate over all components)
        public unsafe Span<T> this[int entityIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var arrayIndex = (ComponentArrayIndex*)(indexData + entityIndex * sizeof(ComponentArrayIndex));

                if (arrayIndex->Count == 0)
                    return Span<T>.Empty;

                var ptr = (T*)(arrayData + arrayIndex->Index * sizeof(T));
                return new Span<T>(ptr, arrayIndex->Count);
            }
        }
    }
}
