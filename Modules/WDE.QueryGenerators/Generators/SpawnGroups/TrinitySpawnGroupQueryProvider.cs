using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath")]
internal class TrinitySpawnGroupQueryProvider : BaseInsertQueryProvider<ISpawnGroupTemplate>, IDeleteQueryProvider<ISpawnGroupTemplate>
{
    protected override object Convert(ISpawnGroupTemplate template)
    {
        return new
            {
                groupId = template.Id,
                groupName = template.Name
            };
    }
    
    public IQuery Delete(ISpawnGroupTemplate t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("groupId") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_template");
}