using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gameobject;

[AutoRegister]
[RequiresCore("TrinityMaster", "CMaNGOS-TBC", "CMaNGOS-Classic", "TrinityCata", "CMaNGOS-WoTLK", "Azeroth", "TrinityWrath")]
internal class GameObjectDeleteQueryProvider : IDeleteQueryProvider<GameObjectSpawnModelEssentials>
{
    public IQuery Delete(GameObjectSpawnModelEssentials t)
    {
        return Queries.Table(DatabaseTable.WorldTable("gameobject"))
            .Where(row => row.Column<uint>("guid") == t.Guid)
            .Delete();
    }
}