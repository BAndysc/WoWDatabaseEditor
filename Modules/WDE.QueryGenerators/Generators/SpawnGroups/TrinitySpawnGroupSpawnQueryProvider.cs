using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath")]
internal class TrinitySpawnGroupSpawnQueryProvider : IInsertQueryProvider<ISpawnGroupSpawn>, IDeleteQueryProvider<ISpawnGroupSpawn>, IDeleteAllQueryProvider<ISpawnGroupSpawn>
{
    public IQuery Insert(ISpawnGroupSpawn spawn)
    {
        if (spawn.Type == SpawnGroupTemplateType.Any)
            throw new ArgumentException("Spawn Type may not be `Any`!");

        return Queries.Table(TableName)
            .Insert(new
            {
                groupId = spawn.TemplateId,
                spawnType = (int)spawn.Type,
                spawnId = spawn.Guid
            });
    }

    public IQuery BulkInsert(IReadOnlyCollection<ISpawnGroupSpawn> collection)
    {
        if (collection.Any(spawn => spawn.Type == SpawnGroupTemplateType.Any))
            throw new ArgumentException("Spawn Type may not be `Any`!");
        
        return Queries.Table(TableName)
            .BulkInsert(collection.Select(spawn => new
            {
                groupId = spawn.TemplateId,
                spawnType = (int)spawn.Type,
                spawnId = spawn.Guid
            }));
    }

    public IQuery Delete(ISpawnGroupSpawn spawn)
    {
        if (spawn.Type == SpawnGroupTemplateType.Any)
            throw new ArgumentException("Spawn Type may not be `Any`!");

        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("groupId") == spawn.TemplateId &&
                          row.Column<uint>("spawnType") == (uint)spawn.Type &&
                          row.Column<uint>("spawnId") == spawn.Guid)
            .Delete();
    }

    /// <summary>Deletes the whole group's membership (any spawn type) - for the idempotent
    /// "rewrite the group" save/export shape (DELETE all, then bulk INSERT).</summary>
    public IQuery DeleteAll(ISpawnGroupSpawn spawn)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("groupId") == spawn.TemplateId)
            .Delete();
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group");
}