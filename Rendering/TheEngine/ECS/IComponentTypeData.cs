using System;

namespace TheEngine.ECS
{
    public interface IComponentTypeData
    {
        Type DataType { get; }
        int SizeBytes { get; }
        int Index { get; }
    }
}