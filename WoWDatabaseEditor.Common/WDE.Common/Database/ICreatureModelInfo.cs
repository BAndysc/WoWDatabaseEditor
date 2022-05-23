namespace WDE.Common.Database;

public interface ICreatureModelInfo
{
    public uint DisplayId { get; }
    public float BoundingRadius { get; }
    public float CombatReach { get; }
    public uint Gender { get; }
}