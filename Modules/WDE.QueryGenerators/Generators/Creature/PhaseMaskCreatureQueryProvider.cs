using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "Azeroth", "TrinityWrath")]
internal class PhaseMaskCreatureQueryProvider : IInsertQueryProvider<CreatureSpawnModelEssentials>
{
    public IQuery Insert(CreatureSpawnModelEssentials t)
    {
        return Queries.Table("creature").Insert(new
        {
            guid = t.Guid,
            id = t.Entry,
            map = t.Map,
            spawnMask = t.SpawnMask,
            phaseMask = t.PhaseMask,
            position_x = t.X,
            position_y = t.Y,
            position_z = t.Z,
            orientation = t.O,
        });
    }

    public string TableName => "creature";
}