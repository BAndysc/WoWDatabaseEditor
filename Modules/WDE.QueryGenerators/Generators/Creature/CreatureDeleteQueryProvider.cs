using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Creature;

[AutoRegister]
[RequiresCore("TrinityMaster", "CMaNGOS-TBC", "CMaNGOS-Classic", "TrinityCata", "CMaNGOS-WoTLK", "Azeroth", "TrinityWrath")]
internal class CreatureDeleteQueryProvider : IDeleteQueryProvider<CreatureSpawnModelEssentials>
{
    public IQuery Delete(CreatureSpawnModelEssentials t)
    {
        return Queries.Table(DatabaseTable.WorldTable("creature"))
            .Where(row => row.Column<uint>("guid") == t.Guid)
            .Delete();
    }
}