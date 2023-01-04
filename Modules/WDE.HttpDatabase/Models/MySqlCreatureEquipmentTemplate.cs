
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonCreatureEquipmentTemplate : ICreatureEquipmentTemplate
{
    
    
    public uint Entry { get; set; }
    
    
    public byte Id { get; set; }
    
    
    public uint Item1 { get; set; }
    
    
    public uint Item2 { get; set; }
    
    
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