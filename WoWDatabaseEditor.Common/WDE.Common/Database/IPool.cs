namespace WDE.Common.Database;

/// <summary>One <c>pool_template</c> row: a spawn pool from which the core activates at most
/// <see cref="MaxLimit"/> members at a time.</summary>
public interface IPoolTemplate
{
    uint Entry { get; }
    /// <summary>Max number of members spawned at once (0 = no limit).</summary>
    uint MaxLimit { get; }
    string Description { get; }
}

/// <summary>Shared shape of a guid-based pool member row. CMaNGOS/AzerothCore keep creatures and
/// gameobjects in separate tables (<c>pool_creature</c>/<c>pool_gameobject</c>) — hence the two
/// derived marker interfaces so a query generator registers per table; Trinity's unified
/// <c>pool_members</c> can later register one provider class implementing both closed generics.</summary>
public interface IPoolSpawnMember
{
    uint Guid { get; }
    uint PoolEntry { get; }
    /// <summary>Explicit roll chance in percent; 0 = equal-chance member.</summary>
    float Chance { get; }
    string? Description { get; }
}

/// <summary>One <c>pool_creature</c> row.</summary>
public interface IPoolCreatureMember : IPoolSpawnMember
{
}

/// <summary>One <c>pool_gameobject</c> row.</summary>
public interface IPoolGameObjectMember : IPoolSpawnMember
{
}

/// <summary>Shared shape of an entry-wide pool member row (<c>pool_creature_template</c>/
/// <c>pool_gameobject_template</c>, CMaNGOS only): every spawn of the entry belongs to the pool.</summary>
public interface IPoolEntryMember
{
    uint Entry { get; }
    uint PoolEntry { get; }
    /// <summary>Explicit roll chance in percent; 0 = equal-chance member.</summary>
    float Chance { get; }
    string? Description { get; }
}

/// <summary>One <c>pool_creature_template</c> row.</summary>
public interface IPoolCreatureEntryMember : IPoolEntryMember
{
}

/// <summary>One <c>pool_gameobject_template</c> row.</summary>
public interface IPoolGameObjectEntryMember : IPoolEntryMember
{
}

/// <summary>One <c>pool_pool</c> row: pool <see cref="PoolId"/> is a member of mother pool
/// <see cref="MotherPool"/> (nested pools).</summary>
public interface IPoolNesting
{
    uint PoolId { get; }
    uint MotherPool { get; }
    /// <summary>Explicit roll chance in percent; 0 = equal-chance member.</summary>
    float Chance { get; }
    string? Description { get; }
}

public class AbstractPoolTemplate : IPoolTemplate
{
    public uint Entry { get; set; }
    public uint MaxLimit { get; set; }
    public string Description { get; set; } = "";
}

public class AbstractPoolCreatureMember : IPoolCreatureMember
{
    public uint Guid { get; set; }
    public uint PoolEntry { get; set; }
    public float Chance { get; set; }
    public string? Description { get; set; }
}

public class AbstractPoolGameObjectMember : IPoolGameObjectMember
{
    public uint Guid { get; set; }
    public uint PoolEntry { get; set; }
    public float Chance { get; set; }
    public string? Description { get; set; }
}

public class AbstractPoolCreatureEntryMember : IPoolCreatureEntryMember
{
    public uint Entry { get; set; }
    public uint PoolEntry { get; set; }
    public float Chance { get; set; }
    public string? Description { get; set; }
}

public class AbstractPoolGameObjectEntryMember : IPoolGameObjectEntryMember
{
    public uint Entry { get; set; }
    public uint PoolEntry { get; set; }
    public float Chance { get; set; }
    public string? Description { get; set; }
}

public class AbstractPoolNesting : IPoolNesting
{
    public uint PoolId { get; set; }
    public uint MotherPool { get; set; }
    public float Chance { get; set; }
    public string? Description { get; set; }
}
