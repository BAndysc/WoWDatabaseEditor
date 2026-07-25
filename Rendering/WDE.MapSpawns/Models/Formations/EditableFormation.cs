using WDE.Common.Database;

namespace WDE.MapSpawns.Models.Formations;

/// <summary>
/// One loaded <c>creature_formations</c> row being edited: a leader↔member follow link. The
/// leader/member guids are the row's identity (memberGUID is the table PK) and are immutable; the
/// follow geometry and group fields are editable. Endpoint world positions are not stored here -
/// they're resolved live from the spawns each frame, so dragging a creature moves its arrows.
/// </summary>
public sealed class EditableFormation : ICreatureFormation
{
    public uint LeaderGuid { get; }
    public uint MemberGuid { get; }

    public float Dist { get; private set; }
    public float Angle { get; private set; }  // degrees (see FormationEditorService)
    public uint GroupAi { get; private set; }
    public uint Point1 { get; private set; }
    public uint Point2 { get; private set; }

    public bool IsDirty { get; private set; }

    /// <summary>A leader's own group row (leaderGUID == memberGUID) - kept so saves round-trip it,
    /// but not drawn or pickable as an arrow.</summary>
    public bool IsLeaderSelfRow => LeaderGuid == MemberGuid;

    public EditableFormation(uint leaderGuid, uint memberGuid, float dist, float angle,
        uint groupAi, uint point1, uint point2, bool dirty = false)
    {
        LeaderGuid = leaderGuid;
        MemberGuid = memberGuid;
        Dist = dist;
        Angle = angle;
        GroupAi = groupAi;
        Point1 = point1;
        Point2 = point2;
        IsDirty = dirty;
    }

    public void SetGeometry(float dist, float angle)
    {
        Dist = dist;
        Angle = angle;
        MarkDirty();
    }

    public void SetParams(float dist, float angle, uint groupAi, uint point1, uint point2)
    {
        Dist = dist;
        Angle = angle;
        GroupAi = groupAi;
        Point1 = point1;
        Point2 = point2;
        MarkDirty();
    }

    public void MarkDirty() => IsDirty = true;
    public void ClearDirty() => IsDirty = false;
}
