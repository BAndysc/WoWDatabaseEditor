using WDE.Module.Attributes;

namespace WDE.QueryGenerators.Base;

/// <summary>
/// Per-core semantic knowledge about the pool tables that isn't expressible through the query
/// providers alone. One implementation per core family ([RequiresCore]), so editor code stays free
/// of core/schema conditionals. Table-level capabilities (nesting via pool_pool, entry-wide
/// pooling via pool_*_template) are detected via the corresponding IQueryGenerator&lt;T&gt;.TableName.
/// </summary>
[NonUniqueProvider]
public interface IPoolSchemaInfoProvider
{
    /// <summary>True when the core only honors explicit member chances at pool_template.max_limit
    /// == 1 and rolls members with equal probability otherwise (CMaNGOS PoolMgr).</summary>
    bool ExplicitChanceRequiresMaxLimitOne { get; }

    /// <summary>True when guid members live in separate per-type tables (pool_creature/
    /// pool_gameobject) rather than a unified pool_members table (Trinity).</summary>
    bool HasSeparatePerTypeMemberTables { get; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosPoolSchemaInfoProvider : IPoolSchemaInfoProvider
{
    public bool ExplicitChanceRequiresMaxLimitOne => true;
    public bool HasSeparatePerTypeMemberTables => true;
}
