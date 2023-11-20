using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gossips;

[AutoRegister]
[SingleInstance]
[RequiresCore( "CMaNGOS-TBC", "CMaNGOS-Classic", "CMaNGOS-WoTLK")]
public class CmangosCreatureGossipQueryProvider : IUpdateQueryProvider<CreatureGossipUpdate>
{
    public IQuery Update(CreatureGossipUpdate diff)
    {
        var update = Queries.Table(TableName)
            .Where(row => row.Column<uint>("Entry") == diff.Entry)
            .ToUpdateQuery();
        
        update = update.Set("GossipMenuId", diff.GossipMenuId);
        
        return update.Update(diff.__comment);
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("creature_template");
}