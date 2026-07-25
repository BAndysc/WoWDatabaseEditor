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
        FreeActionDelegate? FreeAction { get; }

        public delegate void FreeActionDelegate(Engine engine, Span<byte> component);
    }
    
    public interface IManagedComponentTypeData
    {
        Type DataType { get; }
        int Index { get; }
        ulong Hash { get; }
        ulong GlobalHash { get; }
    }
}