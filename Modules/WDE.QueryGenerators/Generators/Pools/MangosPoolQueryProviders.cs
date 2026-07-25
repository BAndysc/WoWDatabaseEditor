using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Pools;

// The CMaNGOS pool tables. Every provider follows the editor's idempotent "rewrite the pool"
// save shape: DeleteAll (all of the pool's rows) + BulkInsert the current state. On cores without
// these tables no provider registers, IQueryGenerator<T>.TableName is null and the editor hides
// the feature (the same capability pattern the spawn-group editor uses). Trinity's unified
// pool_members table can later register one provider class implementing the creature/gameobject/
// nesting generics with the right `type` discriminator.

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosPoolTemplateQueryProvider : BaseInsertQueryProvider<IPoolTemplate>,
    IDeleteQueryProvider<IPoolTemplate>
{
    protected override object Convert(IPoolTemplate template)
    {
        return new
        {
            entry = template.Entry,
            max_limit = template.MaxLimit,
            description = template.Description,
        };
    }

    public IQuery Delete(IPoolTemplate t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("entry") == t.Entry)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("pool_template");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosPoolCreatureQueryProvider : BaseInsertQueryProvider<IPoolCreatureMember>,
    IDeleteQueryProvider<IPoolCreatureMember>, IDeleteAllQueryProvider<IPoolCreatureMember>
{
    protected override object Convert(IPoolCreatureMember member)
    {
        return new
        {
            guid = member.Guid,
            pool_entry = member.PoolEntry,
            chance = member.Chance,
            description = member.Description ?? "", // column is NOT NULL on CMaNGOS
        };
    }

    public IQuery Delete(IPoolCreatureMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("guid") == t.Guid)
            .Delete();
    }

    public IQuery DeleteAll(IPoolCreatureMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("pool_entry") == t.PoolEntry)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("pool_creature");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosPoolGameObjectQueryProvider : BaseInsertQueryProvider<IPoolGameObjectMember>,
    IDeleteQueryProvider<IPoolGameObjectMember>, IDeleteAllQueryProvider<IPoolGameObjectMember>
{
    protected override object Convert(IPoolGameObjectMember member)
    {
        return new
        {
            guid = member.Guid,
            pool_entry = member.PoolEntry,
            chance = member.Chance,
            description = member.Description ?? "",
        };
    }

    public IQuery Delete(IPoolGameObjectMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("guid") == t.Guid)
            .Delete();
    }

    public IQuery DeleteAll(IPoolGameObjectMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("pool_entry") == t.PoolEntry)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("pool_gameobject");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosPoolCreatureTemplateQueryProvider : BaseInsertQueryProvider<IPoolCreatureEntryMember>,
    IDeleteQueryProvider<IPoolCreatureEntryMember>, IDeleteAllQueryProvider<IPoolCreatureEntryMember>
{
    protected override object Convert(IPoolCreatureEntryMember member)
    {
        return new
        {
            id = member.Entry,
            pool_entry = member.PoolEntry,
            chance = member.Chance,
            description = member.Description ?? "",
        };
    }

    public IQuery Delete(IPoolCreatureEntryMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Entry)
            .Delete();
    }

    public IQuery DeleteAll(IPoolCreatureEntryMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("pool_entry") == t.PoolEntry)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("pool_creature_template");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosPoolGameObjectTemplateQueryProvider : BaseInsertQueryProvider<IPoolGameObjectEntryMember>,
    IDeleteQueryProvider<IPoolGameObjectEntryMember>, IDeleteAllQueryProvider<IPoolGameObjectEntryMember>
{
    protected override object Convert(IPoolGameObjectEntryMember member)
    {
        return new
        {
            id = member.Entry,
            pool_entry = member.PoolEntry,
            chance = member.Chance,
            description = member.Description ?? "",
        };
    }

    public IQuery Delete(IPoolGameObjectEntryMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Entry)
            .Delete();
    }

    public IQuery DeleteAll(IPoolGameObjectEntryMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("pool_entry") == t.PoolEntry)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("pool_gameobject_template");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosPoolNestingQueryProvider : BaseInsertQueryProvider<IPoolNesting>,
    IDeleteQueryProvider<IPoolNesting>, IDeleteAllQueryProvider<IPoolNesting>
{
    protected override object Convert(IPoolNesting nesting)
    {
        return new
        {
            pool_id = nesting.PoolId,
            mother_pool = nesting.MotherPool,
            chance = nesting.Chance,
            description = nesting.Description ?? "",
        };
    }

    public IQuery Delete(IPoolNesting t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("pool_id") == t.PoolId)
            .Delete();
    }

    /// <summary>Deletes the pool's nesting rows in BOTH directions - its own mother link and its
    /// children's links - so the idempotent rewrite can reinsert them from current state without
    /// leaving orphans (PoolId carries the pool being rewritten).</summary>
    public IQuery DeleteAll(IPoolNesting t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("pool_id") == t.PoolId || row.Column<uint>("mother_pool") == t.PoolId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("pool_pool");
}
