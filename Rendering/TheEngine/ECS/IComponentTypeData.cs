using System;

namespace TheEngine.ECS
{
    public interface IComponentTypeData
    {
        Type DataType { get; }
        int SizeBytes { get; }
        int Index { get; }
        ulong Hash { get; }
        ulong GlobalHash { get; }
        bool IsArray { get; }
        OnAddedActionDelegate? OnAddedAction { get; }
        OnRemovedActionDelegate? OnRemovedAction { get; }

        /// <summary>Adds a default-valued instance of this component to the entity (as an array element if <see cref="IsArray"/>).</summary>
        void AddDefault(IEntityManager em, Entity entity);

        public delegate void OnAddedActionDelegate(Engine engine, Entity entity, Span<byte> component);
        public delegate void OnRemovedActionDelegate(Engine engine, Entity entity, Span<byte> component);
    }

    public interface IManagedComponentTypeData
    {
        Type DataType { get; }
        int Index { get; }
        ulong Hash { get; }
        ulong GlobalHash { get; }

        /// <summary>Constructs (via its parameterless constructor) and adds an instance of this managed component to the entity.</summary>
        void AddDefault(IEntityManager em, Entity entity);
    }
}