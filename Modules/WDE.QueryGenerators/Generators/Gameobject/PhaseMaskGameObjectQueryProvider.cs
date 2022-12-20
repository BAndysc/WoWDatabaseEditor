using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gameobject;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "Azeroth", "TrinityWrath")]
internal class PhaseMaskGameObjectQueryProvider : IInsertQueryProvider<GameObjectSpawnModelEssentials>
{
    public IQuery Insert(GameObjectSpawnModelEssentials t)
    {
        return Queries.Table("gameobject").Insert(new
        {
            guid = t.Guid,
            id = t.Entry,
            map = t.Map,
            spawnMask = t.SpawnMask,
            phaseMask = t.PhaseMask,
            position_x = t.X,
            position_y = t.Y,
            position_z = t.Z,
            state = t.State,
            rotation0 = t.Rotation0,
            rotation1 = t.Rotation1,
            rotation2 = t.Rotation2,
            rotation3 = t.Rotation3,
        });
    }

    public string TableName => "gameobject";
}