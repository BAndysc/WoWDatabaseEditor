using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gossips;

[AutoRegister]
[SingleInstance]
[RequiresCore( "TrinityCata", "Azeroth", "TrinityWrath")]
public class CreatureGossipQueryProvider : IUpdateQueryProvider<CreatureGossipUpdate>
{
    public IQuery Update(CreatureGossipUpdate diff)
    {
        var update = Queries.Table(TableName)
            .Where(row => row.Column<uint>("entry") == diff.Entry)
            .ToUpdateQuery();
        
        update = update.Set("gossip_menu_id", diff.GossipMenuId);
        
        return update.Update(diff.__comment);
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("creature_template");
}