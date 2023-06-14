using System.Numerics;

namespace WDE.QueryGenerators.Models;

public struct CreatureDiff
{
    public uint Guid { get; init; }
    public uint Entry { get; init; }
    public Vector3? Position { get; init; }
    public float? Orientation { get; init; }
}