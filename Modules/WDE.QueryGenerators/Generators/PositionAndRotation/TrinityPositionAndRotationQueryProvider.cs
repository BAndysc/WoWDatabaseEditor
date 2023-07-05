using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Quests;

[AutoRegister]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "AzerothCore")]
public class TrinityPositionAndRotationQueryProvider : IUpdateQueryProvider<PositionAndRotationDiff>
{
    private readonly IDatabaseProvider databaseProvider;

    public TrinityPositionAndRotationQueryProvider(IDatabaseProvider databaseProvider, ICurrentCoreVersion currentCoreVersion)
    {
        this.databaseProvider = databaseProvider;
    }
    
    public IQuery Update(PositionAndRotationDiff par)
    {
        var trans = Queries.BeginTransaction();

        var creatureEntry = databaseProvider.GetCreatureByGuid(par.Guid);
        var gameObjectEntry = databaseProvider.GetGameObjectByGuid(par.Guid);

        if (creatureEntry != null)
            trans.Comment(creatureEntry.Guid.ToString());
        if (gameObjectEntry != null)
            trans.Comment(gameObjectEntry.Guid.ToString());

        var update = trans.Table(par.Type)
            .Where(row => row.Column<uint>("Guid") == par.Guid)
            .ToUpdateQuery();

        update = update.Set("position_x", par.Position.X);
        update = update.Set("position_y", par.Position.Y);
        update = update.Set("position_z", par.Position.Z);

        update = update.Set("orientation", par.Orientation);

        if (gameObjectEntry != null && par.Rotation.HasValue)
        {
            update = update.Set("rotation0", par.Rotation?.X);
            update = update.Set("rotation1", par.Rotation?.Y);
            update = update.Set("rotation2", par.Rotation?.Z);
            update = update.Set("rotation3", par.Rotation?.W);
        }

        update.Update();

        return trans.Close();
    }
}