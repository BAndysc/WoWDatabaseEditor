using TheEngine.ECS;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers.Entities;

public struct Adt_M2Object : IComponentData
{
    public Vector3 AbsolutePosition;
    public Vector3 Rotation;
    public float Scale;
    public MDDFFlags Flags;
    public M2Id Id;
}
