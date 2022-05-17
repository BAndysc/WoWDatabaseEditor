using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "creature_equip_template")]
public class MySqlCreatureEquipmentTemplate : ICreatureEquipmentTemplate
{
    [PrimaryKey]
    [Column(Name = "CreatureID")]
    public uint Entry { get; set; }
    
    [Column(Name = "ID")]
    public byte Id { get; set; }
    
    [Column(Name = "ItemID1")]
    public uint Item1 { get; set; }
    
    [Column(Name = "ItemID2")]
    public uint Item2 { get; set; }
    
    [Column(Name = "ItemID3")]
    public uint Item3 { get; set; }
    
    
    public uint GetItem(int id)
    {
        switch (id)
        {
            case 0:
                return Item1;
            case 1:
                return Item2;
            case 2:
                return Item3;
        }

        return 0;
    }
}