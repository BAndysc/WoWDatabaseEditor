using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gameobject;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "Azeroth", "CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class TrinityGameObjectDiffQueryProvider : IUpdateQueryProvider<GameObjectDiff>
{
    private readonly ICachedDatabaseProvider databaseProvider;

    public TrinityGameObjectDiffQueryProvider(ICachedDatabaseProvider databaseProvider)
    {
        this.databaseProvider = databaseProvider;
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("gameobject");
    
    public IQuery Update(GameObjectDiff diff)
    {
        var trans = Queries.BeginTransaction(DataDatabaseType.World);

        var gameObjectEntry = databaseProvider.GetCachedGameObjectByGuid(diff.Guid, diff.Entry);

        if (gameObjectEntry != null)
            trans.Comment(gameObjectEntry.Guid.ToString());

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

        if (diff.Rotation is { } rotation)
        {
            update = update.Set("rotation0", rotation.X);
            update = update.Set("rotation1", rotation.Y);
            update = update.Set("rotation2", rotation.Z);
            update = update.Set("rotation3", rotation.W);
        }

        update.Update();

        return trans.Close();
    }
}