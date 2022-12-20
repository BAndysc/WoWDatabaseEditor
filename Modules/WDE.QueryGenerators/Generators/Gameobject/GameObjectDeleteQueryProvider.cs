using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gameobject;

[AutoRegister]
internal class GameObjectDeleteQueryProvider : IDeleteQueryProvider<GameObjectSpawnModelEssentials>
{
    public IQuery Delete(GameObjectSpawnModelEssentials t)
    {
        return Queries.Table("gameobject")
            .Where(row => row.Column<uint>("guid") == t.Guid)
            .Delete();
    }
}