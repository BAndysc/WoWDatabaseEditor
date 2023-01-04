using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "Azeroth", "CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class TrinityCreatureDiffQueryProvider : IUpdateQueryProvider<CreatureDiff>
{
    private readonly ICachedDatabaseProvider databaseProvider;

    public TrinityCreatureDiffQueryProvider(ICachedDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("creature");
    
    public IQuery Update(CreatureDiff diff)
    {
        var trans = Queries.BeginTransaction(DataDatabaseType.World);

        var creatureEntry = databaseProvider.GetCachedCreatureByGuid(diff.Guid, diff.Entry);

        if (creatureEntry != null)
            trans.Comment(creatureEntry.Guid.ToString());

        var update = trans.Table(TableName)
            .Where(row => row.Column<uint>("Guid") == diff.Guid)
            .ToUpdateQuery();

        if (diff.Position is {} pos)
        {
            update = update.Set("position_x", pos.X)
                           .Set("position_y", pos.Y)
                           .Set("position_z", pos.Z);
        }

        if (diff.Orientation is { } orientation)
            update = update.Set("orientation", orientation);
        
        update.Update();

        return trans.Close();
    }
}