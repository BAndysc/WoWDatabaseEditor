using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[RejectsCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class TrinitySpawnGroupQueryProvider : IInsertQueryProvider<ISpawnGroupTemplate>, IDeleteQueryProvider<ISpawnGroupTemplate>
{
    public IQuery Insert(ISpawnGroupTemplate template)
    {
        return Queries.Table("spawn_group_template")
            .Insert(new
            {
                groupId = template.Id,
                groupName = template.Name
            });
    }
    
    public IQuery Delete(ISpawnGroupTemplate t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("groupId") == t.Id)
            .Delete();
    }

    public string TableName => "spawn_group_template";
}