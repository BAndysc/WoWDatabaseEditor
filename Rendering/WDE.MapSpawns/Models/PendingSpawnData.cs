using WDE.Common.Database;
using WDE.Common.Utils;

namespace WDE.MapSpawns.Models;

/// <summary>Backing "DB row" for a creature spawned this session (no real row exists until Save).
/// Mutable transform so drag-moves keep the tree/chunk bookkeeping consistent.</summary>
public class PendingCreatureData : ICreature
{
    public uint Guid { get; init; }
    public uint Entry { get; init; }
    public int Map { get; init; }
    public uint? PhaseMask => null;
    public SmallReadOnlyList<int>? PhaseId => null;
    public int? PhaseGroup => null;
    public int EquipmentId => 0;
    public uint Model => 0;
    public MovementType MovementType => MovementType.Idle;
    public float WanderDistance => 0;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float O { get; set; }
}

/// <summary>Backing "DB row" for a gameobject spawned this session (see <see cref="PendingCreatureData"/>).</summary>
public class PendingGameObjectData : IGameObject
{
    public uint Guid { get; init; }
    public uint Entry { get; init; }
    public int Map { get; init; }
    public uint? PhaseMask => null;
    public SmallReadOnlyList<int>? PhaseId => null;
    public int? PhaseGroup => null;
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Orientation { get; set; }
    public float Rotation0 => 0;
    public float Rotation1 => 0;
    public float Rotation2 => MathF.Sin(Orientation / 2);
    public float Rotation3 => MathF.Cos(Orientation / 2);
    public float ParentRotation0 => 0;
    public float ParentRotation1 => 0;
    public float ParentRotation2 => 0;
    public float ParentRotation3 => 0;
}
