using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "creature_equip_template")]
public class CreatureEquipmentTemplate : IMangosCreatureEquipmentTemplate
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "equipentry1")]
    public uint Item1 { get; set; }
    
    [Column(Name = "equipentry2")]
    public uint Item2 { get; set; }
    
    [Column(Name = "equipentry3")]
    public uint Item3 { get; set; }

    public uint GetItem(int id)
    {
        if (id == 0)
            return Item1;
        if (id == 1)
            return Item2;
        if (id == 2)
            return Item3;
        return 0;
    }
}