using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gossips;

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster", "TrinityCata", "TrinityWrath", "Azeroth")]
public class GossipMenuQueryProvider : BaseInsertQueryProvider<IGossipMenuLine>, IDeleteQueryProvider<IGossipMenuLine>
{
    protected override object Convert(IGossipMenuLine menu)
    {
        return new
        {
            MenuID = menu.MenuId,
            TextID = menu.TextId,
            __comment = menu.__comment
        };
    }
    
    public IQuery Delete(IGossipMenuLine t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("MenuID") == t.MenuId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("gossip_menu");
}