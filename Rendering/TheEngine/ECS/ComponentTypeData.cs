using System;

namespace TheEngine.ECS
{
    public class ComponentTypeData<T> : IComponentTypeData<T> where T : unmanaged, IComponentData
    {

        public unsafe ComponentTypeData(int index)
        {
            Index = index;
            DataType = typeof(T);
            SizeBytes = sizeof(T);
        }

        public int Index { get; }
        public Type DataType { get; }
        public int SizeBytes { get; }
    }
    
    public class ManagedComponentTypeData<T> : IManagedComponentTypeData<T> where T : class, IManagedComponentData
    {
        public unsafe ManagedComponentTypeData(int index)
        {
            Index = index;
            DataType = typeof(T);
        }

        public int Index { get; }
        public Type DataType { get; }
    }
}