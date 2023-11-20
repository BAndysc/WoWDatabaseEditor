using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupQueryProvider : IInsertQueryProvider<ISpawnGroupTemplate>, IDeleteQueryProvider<ISpawnGroupTemplate>
{
    public IQuery Insert(ISpawnGroupTemplate template)
    {
        if (template.Type == SpawnGroupTemplateType.Any)
            throw new ArgumentException("Template Type may not be `Any`!");
        
        return Queries.Table(TableName)
            .Insert(new
            {
                Id = template.Id,
                Name = template.Name,
                Type = (int)template.Type
            });
    }

    public IQuery BulkInsert(IReadOnlyCollection<ISpawnGroupTemplate> collection)
    {
        if (collection.Any(template => template.Type == SpawnGroupTemplateType.Any))
            throw new ArgumentException("Template Type may not be `Any`!");

        return Queries.Table(TableName)
            .BulkInsert(collection.Select(template => new
            {
                Id = template.Id,
                Name = template.Name,
                Type = (int)template.Type
            }));
    }

    public IQuery Delete(ISpawnGroupTemplate t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.Id)
            .Delete();
    }
    
    public DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group");
}