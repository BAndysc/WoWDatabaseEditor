using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gossips;

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster")]
public class MasterCreatureGossipQueryProvider : IUpdateQueryProvider<CreatureGossipUpdate>
{
    public IQuery Update(CreatureGossipUpdate diff)
    {
        var trans = Queries.BeginTransaction(DataDatabaseType.World);
        trans.Table(TableName)
            .Where(row => row.Column<uint>("CreatureID") == diff.Entry &&
                          row.Column<uint>("MenuID") == diff.GossipMenuId)
            .Delete();

        trans.Table(TableName)
            .Insert(new
            {
                CreatureID = diff.Entry,
                MenuID = diff.GossipMenuId
            });
        
        return trans.Close();
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("creature_template_gossip");
}