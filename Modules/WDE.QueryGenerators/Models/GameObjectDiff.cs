namespace WDE.QueryGenerators.Models;

using System.Numerics;

public struct GameObjectDiff
{
    public uint Guid { get; init; }
    public uint Entry { get; init; }
    public Vector3? Position { get; init; }
    public float? Orientation { get; init; }
    public Quaternion? Rotation { get; set; }
}