using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupQueryProvider : IInsertQueryProvider<ISpawnGroupTemplate>, IDeleteQueryProvider<ISpawnGroupTemplate>
{
    // full row when the editor passes the advanced model; safe defaults otherwise
    private static object Convert(ISpawnGroupTemplate template)
    {
        if (template.Type == SpawnGroupTemplateType.Any)
            throw new ArgumentException("Template Type may not be `Any`!");

        var adv = template as ISpawnGroupTemplateAdvanced;
        return new
        {
            Id = template.Id,
            Name = template.Name,
            Type = (int)template.Type,
            MaxCount = adv?.MaxCount ?? 0,
            WorldState = adv?.WorldState ?? 0,
            WorldStateExpression = adv?.WorldStateExpression ?? 0,
            Flags = template.MangosFlags ?? 0,
            StringId = adv?.StringId ?? 0,
            RespawnOverrideMin = adv?.RespawnOverrideMin,
            RespawnOverrideMax = adv?.RespawnOverrideMax,
        };
    }

    public IQuery Insert(ISpawnGroupTemplate template)
    {
        return Queries.Table(TableName).Insert(Convert(template));
    }

    public IQuery BulkInsert(IReadOnlyCollection<ISpawnGroupTemplate> collection)
    {
        return Queries.Table(TableName).BulkInsert(collection.Select(Convert));
    }

    public IQuery Delete(ISpawnGroupTemplate t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.Id)
            .Delete();
    }
    
    public DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group");
}