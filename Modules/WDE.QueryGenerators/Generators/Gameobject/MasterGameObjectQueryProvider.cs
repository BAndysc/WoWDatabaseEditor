using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gameobject;

[AutoRegister]
[RequiresCore("TrinityMaster")]
internal class MasterGameObjectQueryProvider : BaseInsertQueryProvider<GameObjectSpawnModelEssentials>
{
    protected override object Convert(GameObjectSpawnModelEssentials t)
    {
        return new
        {
            guid = t.Guid,
            id = t.Entry,
            map = t.Map,
            phaseId = t.PhaseId.FirstOrDefault(),
            position_x = t.X,
            position_y = t.Y,
            position_z = t.Z,
            state = t.State,
            rotation0 = t.Rotation0,
            rotation1 = t.Rotation1,
            rotation2 = t.Rotation2,
            rotation3 = t.Rotation3,
        };
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("gameobject");
}