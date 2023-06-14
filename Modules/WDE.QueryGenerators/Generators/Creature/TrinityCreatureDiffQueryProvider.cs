using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "AzerothCore", "CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class TrinityCreatureDiffQueryProvider : IUpdateQueryProvider<CreatureDiff>
{
    private readonly IDatabaseProvider databaseProvider;

    public TrinityCreatureDiffQueryProvider(IDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }

    public string TableName => "creature";
    
    public IQuery Update(CreatureDiff diff)
    {
        var trans = Queries.BeginTransaction();

        var creatureEntry = databaseProvider.GetCreatureByGuid(diff.Guid, diff.Entry);

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