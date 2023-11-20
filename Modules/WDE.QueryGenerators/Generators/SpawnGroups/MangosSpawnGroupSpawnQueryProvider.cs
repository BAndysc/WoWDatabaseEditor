using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupSpawnQueryProvider : BaseInsertQueryProvider<ISpawnGroupSpawn>, IDeleteQueryProvider<ISpawnGroupSpawn> 
{
    protected override object Convert(ISpawnGroupSpawn spawn)
    {
        return new
            {
                Id = spawn.TemplateId,
                Guid = spawn.Guid
            };
    }

    public IQuery Delete(ISpawnGroupSpawn t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.TemplateId &&
                          row.Column<uint>("Guid") == t.Guid)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_spawn");
}