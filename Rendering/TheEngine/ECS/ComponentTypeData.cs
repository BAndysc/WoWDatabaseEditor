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
        public ulong Hash => (ulong)(1 << Index);
        public ulong GlobalHash => Hash;
        public Type DataType { get; }
        public int SizeBytes { get; }

        protected bool Equals(ComponentTypeData<T> other)
        {
            return Index == other.Index && DataType == other.DataType;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ComponentTypeData<T>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, DataType);
        }

        public static bool operator ==(ComponentTypeData<T>? left, ComponentTypeData<T>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ComponentTypeData<T>? left, ComponentTypeData<T>? right)
        {
            return !Equals(left, right);
        }
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
        public ulong Hash => (ulong)(1 << Index);
        public ulong GlobalHash => Hash << 32;

        protected bool Equals(ManagedComponentTypeData<T> other)
        {
            return Index == other.Index && DataType == other.DataType;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ManagedComponentTypeData<T>)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, DataType);
        }

        public static bool operator ==(ManagedComponentTypeData<T>? left, ManagedComponentTypeData<T>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ManagedComponentTypeData<T>? left, ManagedComponentTypeData<T>? right)
        {
            return !Equals(left, right);
        }
    }
}