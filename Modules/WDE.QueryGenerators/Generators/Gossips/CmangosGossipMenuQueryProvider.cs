using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gossips;

[AutoRegister]
[SingleInstance]
[RequiresCore( "CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class CmangosGossipMenuQueryProvider : BaseInsertQueryProvider<IGossipMenuLine>, IDeleteQueryProvider<IGossipMenuLine>
{
    protected override object Convert(IGossipMenuLine menu)
    {
        return new
        {
            entry = menu.MenuId,
            text_id = menu.TextId,
            __comment = menu.__comment
        };
    }
    
    public IQuery Delete(IGossipMenuLine t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("entry") == t.MenuId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("gossip_menu");
}