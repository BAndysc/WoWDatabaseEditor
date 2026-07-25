using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TheEngine.ECS
{
    public class ArrayComponentAttribute : Attribute
    {
    }

    /// <summary>Matched by name against a public static method on a component struct (see <see cref="ComponentTypeData{T}"/>).</summary>
    public delegate void OnAddedDelegate<T>(Engine engine, Entity entity, ref T component) where T : unmanaged, IComponentData;
    /// <summary>Matched by name against a public static method on a component struct (see <see cref="ComponentTypeData{T}"/>).</summary>
    public delegate void OnRemovedDelegate<T>(Engine engine, Entity entity, ref T component) where T : unmanaged, IComponentData;

    public class ComponentTypeData<T> : IComponentTypeData where T : unmanaged, IComponentData
    {
        public ComponentTypeData(int index)
        {
            Index = index;
            DataType = typeof(T);
            // must be the managed size: ComponentDataAccess/ComponentArrayDataAccess index the storage
            // with sizeof(T) strides, and Marshal.SizeOf can differ (e.g. bool fields marshal as 4 bytes)
            SizeBytes = Unsafe.SizeOf<T>();
            IsArray = typeof(T).GetCustomAttributes(typeof(ArrayComponentAttribute), false).Length > 0;
            OnAddedAction = BindOnAdded();
            OnRemovedAction = BindOnRemoved();
        }

        // T is already concrete here, so this stays Native AOT safe (no MakeGenericMethod needed)
        private static MethodInfo? FindHookMethod(string name)
        {
            return typeof(T).GetMethod(name, BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Engine), typeof(Entity), typeof(T).MakeByRefType() }, null);
        }

        private static IComponentTypeData.OnAddedActionDelegate? BindOnAdded()
        {
            var method = FindHookMethod("OnAdded");
            if (method == null)
                return null;
            var typed = (OnAddedDelegate<T>)method.CreateDelegate(typeof(OnAddedDelegate<T>));
            return (engine, entity, bytes) => typed(engine, entity, ref MemoryMarshal.Cast<byte, T>(bytes)[0]);
        }

        private static IComponentTypeData.OnRemovedActionDelegate? BindOnRemoved()
        {
            var method = FindHookMethod("OnRemoved");
            if (method == null)
                return null;
            var typed = (OnRemovedDelegate<T>)method.CreateDelegate(typeof(OnRemovedDelegate<T>));
            return (engine, entity, bytes) => typed(engine, entity, ref MemoryMarshal.Cast<byte, T>(bytes)[0]);
        }

        public int Index { get; }
        public ulong Hash => 1ul << Index;
        public ulong GlobalHash => Hash;
        public bool IsArray { get; }
        public IComponentTypeData.OnAddedActionDelegate? OnAddedAction { get; }
        public IComponentTypeData.OnRemovedActionDelegate? OnRemovedAction { get; }
        public Type DataType { get; }
        public int SizeBytes { get; }

        public void AddDefault(IEntityManager em, Entity entity)
        {
            // CreateInstance, unlike default(T), runs T's parameterless constructor if it defines one
            var value = Activator.CreateInstance<T>();
            if (IsArray)
                em.AddArrayComponent<T>(entity, value);
            else
                em.AddComponent<T>(entity, value);
        }

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

        public void AddDefault(IEntityManager em, Entity entity)
        {
            em.AddManagedComponent<T>(entity, Activator.CreateInstance<T>());
        }

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