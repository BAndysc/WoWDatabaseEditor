using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Gossips;

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster")]
public class TrinityMasterGossipMenuOptionQueryProvider : BaseInsertQueryProvider<IGossipMenuOption>, IDeleteQueryProvider<IGossipMenuOption>
{
    protected override object Convert(IGossipMenuOption option)
    {
        return new
        {
            MenuID = option.MenuId,
            OptionID = option.OptionIndex,
            //OptionIcon = (int)option.Icon, // no icon in TrinityMaster
            OptionText = option.Text,
            OptionBroadcastTextID = option.BroadcastTextId,
            OptionNpc = (uint)option.OptionType,
            //OptionNpcFlag = option.NpcFlag, // no npc flag in TrinityMaster
            ActionMenuID = option.ActionMenuId,
            ActionPoiID = option.ActionPoiId,
            BoxCoded = option.BoxCoded,
            BoxMoney = option.BoxMoney,
            BoxText = option.BoxText,
            BoxBroadcastTextID = option.BoxBroadcastTextId,
            __comment = option.__comment,
            __ignored = option.__ignored
        };
    }
    
    public IQuery Delete(IGossipMenuOption t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("MenuID") == t.MenuId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("gossip_menu_option");
}