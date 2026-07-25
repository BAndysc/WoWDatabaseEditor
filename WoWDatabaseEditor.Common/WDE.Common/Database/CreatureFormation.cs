namespace WDE.Common.Database;

/// <summary>
/// One row of TrinityCore's <c>creature_formations</c> table: a leader↔member follow relationship.
/// The leader's own group is defined by a row where leaderGUID == memberGUID. Members reference
/// their leader's guid and carry the follow geometry (distance + angle relative to the leader).
/// </summary>
public interface ICreatureFormation
{
    uint LeaderGuid { get; }
    uint MemberGuid { get; }
    float Dist { get; }
    float Angle { get; }
    uint GroupAi { get; }
    uint Point1 { get; }
    uint Point2 { get; }
}
