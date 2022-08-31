namespace WDE.Common.Database;

public interface ISpawnGroupFormation
{
    uint Id { get; }
    FormationShape FormationType { get; }
    float Spread { get; }
    int Options { get; }
    int PathId { get; }
    MovementType MovementType { get; }
    string? Comment { get; }
}

public enum FormationShape
{
    Formation0 = 0,
    Formation1 = 1,
    Formation2 = 2,
    Formation3 = 3,
    Formation4 = 4,
    Formation5 = 5,
    Formation6 = 6,
}