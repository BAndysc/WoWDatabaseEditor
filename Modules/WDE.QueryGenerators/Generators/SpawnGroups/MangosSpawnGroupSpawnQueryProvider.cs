using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupSpawnQueryProvider : BaseInsertQueryProvider<ISpawnGroupSpawn>, IDeleteQueryProvider<ISpawnGroupSpawn>, IDeleteAllQueryProvider<ISpawnGroupSpawn>
{
    protected override object Convert(ISpawnGroupSpawn spawn)
    {
        return new
            {
                Id = spawn.TemplateId,
                Guid = spawn.Guid,
                SlotId = spawn.SlotId ?? -1, // 0 = formation leader, -1 = not part of the formation
                Chance = spawn.Chance ?? 0
            };
    }

    public IQuery Delete(ISpawnGroupSpawn t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.TemplateId &&
                          row.Column<uint>("Guid") == t.Guid)
            .Delete();
    }

    /// <summary>Deletes the whole group's membership - for the idempotent "rewrite the group"
    /// save/export shape (DELETE all, then bulk INSERT).</summary>
    public IQuery DeleteAll(ISpawnGroupSpawn t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.TemplateId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_spawn");
}