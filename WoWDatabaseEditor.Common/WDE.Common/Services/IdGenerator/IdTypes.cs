namespace WDE.Common.Services.IdGenerator;

[IdTypeName("Creature guid")]
public sealed class CreatureGuidIdType : IIdType
{
    /// <summary>Entry of the creature being spawned; optional, some sources may use it.</summary>
    public uint Entry { get; init; }
}

[IdTypeName("Gameobject guid")]
public sealed class GameObjectGuidIdType : IIdType
{
    /// <summary>Entry of the gameobject being spawned; optional, some sources may use it.</summary>
    public uint Entry { get; init; }
}

[IdTypeName("Condition entry (mangos)")]
public sealed class MangosConditionEntryIdType : IIdType
{
    /// <summary>Highest condition_entry already used by the caller's unsaved document.</summary>
    public long LocalMax { get; init; }
}
