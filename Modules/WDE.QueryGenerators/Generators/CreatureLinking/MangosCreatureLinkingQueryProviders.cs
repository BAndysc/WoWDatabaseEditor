using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.CreatureLinking;

// The CMaNGOS creature linking tables. The editor's save shape is an idempotent per-row rewrite:
// Delete (by primary key) + Insert the current state; a removed link is just the Delete. On cores
// without these tables no provider registers, IQueryGenerator<T>.TableName is null and the editor
// hides the feature (the same capability pattern the spawn-group and pool editors use).

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosCreatureLinkingQueryProvider : BaseInsertQueryProvider<ICreatureLinking>,
    IDeleteQueryProvider<ICreatureLinking>
{
    protected override object Convert(ICreatureLinking link)
    {
        return new
        {
            guid = link.Guid,
            master_guid = link.MasterGuid,
            flag = link.Flag,
        };
    }

    public IQuery Delete(ICreatureLinking t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("guid") == t.Guid)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature_linking");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosCreatureLinkingTemplateQueryProvider : BaseInsertQueryProvider<ICreatureLinkingTemplate>,
    IDeleteQueryProvider<ICreatureLinkingTemplate>
{
    protected override object Convert(ICreatureLinkingTemplate link)
    {
        return new
        {
            entry = link.Entry,
            map = link.Map,
            master_entry = link.MasterEntry,
            flag = link.Flag,
            search_range = link.SearchRange,
        };
    }

    public IQuery Delete(ICreatureLinkingTemplate t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("entry") == t.Entry && row.Column<uint>("map") == t.Map)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature_linking_template");
}
