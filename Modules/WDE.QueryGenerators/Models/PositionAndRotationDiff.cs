namespace WDE.QueryGenerators.Models;

using System.Numerics;

public struct PositionAndRotationDiff
{
    public uint Guid { get; init; }
    public string Type { get; init; }
    public Vector3 Position { get; init; }
    public float Orientation { get; init; }
    public Quaternion? Rotation { get; set; }
}