using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TheEngine.Components;
using TheEngine.Entities;

namespace TheEngine.ECS
{
    public class ArrayComponentAttribute : Attribute
    {
    }

    public class ComponentTypeData<T> : IComponentTypeData where T : unmanaged, IComponentData
    {
        public delegate void FreeDelegate(Engine engine, ref T component);

        public ComponentTypeData(int index)
        {
            Index = index;
            DataType = typeof(T);
            // must be the managed size: ComponentDataAccess/ComponentArrayDataAccess index the storage
            // with sizeof(T) strides, and Marshal.SizeOf can differ (e.g. bool fields marshal as 4 bytes)
            SizeBytes = Unsafe.SizeOf<T>();
            IsArray = typeof(T).GetCustomAttributes(typeof(ArrayComponentAttribute), false).Length > 0;
            // todo: more generic way to specify FreeAction
            if (typeof(T) == typeof(MeshRenderer))
            {
                FreeAction = (engine, bytes) =>
                {
                    var meshRenderer = MemoryMarshal.Cast<byte, MeshRenderer>(bytes);
                    meshRenderer[0].Mesh = null;
                    meshRenderer[0].Material = null;
                };
            }
        }

        public int Index { get; }
        public ulong Hash => 1ul << Index;
        public ulong GlobalHash => Hash;
        public bool IsArray { get; }
        public IComponentTypeData.FreeActionDelegate? FreeAction { get; set; }
        public Type DataType { get; }
        public int SizeBytes { get; }
        public FreeDelegate? Free { get; }

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
        public ulong Hash => 1ul << Index;
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