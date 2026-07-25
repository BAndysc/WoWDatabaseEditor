using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "pool_template")]
public class PoolTemplate : IPoolTemplate
{
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint Entry { get; set; }

    [Column(Name = "max_limit")]
    public uint MaxLimit { get; set; }

    [Column(Name = "description")]
    public string Description { get; set; } = "";
}

[Table(Name = "pool_creature")]
public class PoolCreature : IPoolCreatureMember
{
    [PrimaryKey]
    [Column(Name = "guid")]
    public uint Guid { get; set; }

    [Column(Name = "pool_entry")]
    public uint PoolEntry { get; set; }

    [Column(Name = "chance")]
    public float Chance { get; set; }

    [Column(Name = "description")]
    public string? Description { get; set; }
}

[Table(Name = "pool_gameobject")]
public class PoolGameObject : IPoolGameObjectMember
{
    [PrimaryKey]
    [Column(Name = "guid")]
    public uint Guid { get; set; }

    [Column(Name = "pool_entry")]
    public uint PoolEntry { get; set; }

    [Column(Name = "chance")]
    public float Chance { get; set; }

    [Column(Name = "description")]
    public string? Description { get; set; }
}

[Table(Name = "pool_creature_template")]
public class PoolCreatureTemplate : IPoolCreatureEntryMember
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Entry { get; set; }

    [Column(Name = "pool_entry")]
    public uint PoolEntry { get; set; }

    [Column(Name = "chance")]
    public float Chance { get; set; }

    [Column(Name = "description")]
    public string? Description { get; set; }
}

[Table(Name = "pool_gameobject_template")]
public class PoolGameObjectTemplate : IPoolGameObjectEntryMember
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Entry { get; set; }

    [Column(Name = "pool_entry")]
    public uint PoolEntry { get; set; }

    [Column(Name = "chance")]
    public float Chance { get; set; }

    [Column(Name = "description")]
    public string? Description { get; set; }
}

[Table(Name = "pool_pool")]
public class PoolPool : IPoolNesting
{
    [PrimaryKey]
    [Column(Name = "pool_id")]
    public uint PoolId { get; set; }

    [Column(Name = "mother_pool")]
    public uint MotherPool { get; set; }

    [Column(Name = "chance")]
    public float Chance { get; set; }

    [Column(Name = "description")]
    public string? Description { get; set; }
}
