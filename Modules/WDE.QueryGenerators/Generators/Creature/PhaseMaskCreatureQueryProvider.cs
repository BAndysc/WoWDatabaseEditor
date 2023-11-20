using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "TrinityWrath")]
internal class PhaseMaskCreatureQueryProvider : BaseInsertQueryProvider<CreatureSpawnModelEssentials>
{
    protected override object Convert(CreatureSpawnModelEssentials t)
    {
        return new
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
        };
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature");
}


[AutoRegister]
[RequiresCore("Azeroth")]
internal class AzerothCreatureQueryProvider : BaseInsertQueryProvider<CreatureSpawnModelEssentials>
{
    protected override object Convert(CreatureSpawnModelEssentials t)
    {
        return new
        {
            guid = t.Guid,
            id1 = t.Entry,
            map = t.Map,
            spawnMask = t.SpawnMask,
            phaseMask = t.PhaseMask,
            position_x = t.X,
            position_y = t.Y,
            position_z = t.Z,
            orientation = t.O,
        };
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature");
}