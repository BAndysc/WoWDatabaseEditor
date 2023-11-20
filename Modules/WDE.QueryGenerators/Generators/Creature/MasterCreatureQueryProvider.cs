using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
[RequiresCore("TrinityMaster")]
internal class MasterCreatureQueryProvider : BaseInsertQueryProvider<CreatureSpawnModelEssentials>
{
    protected override object Convert(CreatureSpawnModelEssentials t)
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
            orientation = t.O,
        };
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature");
}