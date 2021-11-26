using System;

namespace TheEngine.ECS
{
    public interface IComponentTypeData
    {
        Type DataType { get; }
        int SizeBytes { get; }
        int Index { get; }
    }
    
    public interface IManagedComponentTypeData
    {
        Type DataType { get; }
        int Index { get; }
    }
}