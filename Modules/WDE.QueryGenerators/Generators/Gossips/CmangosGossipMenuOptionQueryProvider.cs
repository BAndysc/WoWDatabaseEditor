using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gossips;

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class CmangosGossipMenuOptionQueryProvider : BaseInsertQueryProvider<IGossipMenuOption>, IDeleteQueryProvider<IGossipMenuOption>
{
    protected override object Convert(IGossipMenuOption option)
    {
        return new
        {
            menu_id = option.MenuId,
            id = option.OptionIndex,
            option_icon = (int)option.Icon,
            option_text = option.Text,
            option_broadcast_text = option.BroadcastTextId,
            option_id = (uint)option.OptionType,
            npc_option_npcflag = option.NpcFlag,
            action_menu_id = option.ActionMenuId,
            action_poi_id = option.ActionPoiId,
            box_coded = option.BoxCoded,
            box_money = option.BoxMoney,
            box_text = option.BoxText,
            box_broadcast_text = option.BoxBroadcastTextId,
            __comment = option.__comment,
            __ignored = option.__ignored
        };
    }
    
    public IQuery Delete(IGossipMenuOption t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("menu_id") == t.MenuId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("gossip_menu_option");
}