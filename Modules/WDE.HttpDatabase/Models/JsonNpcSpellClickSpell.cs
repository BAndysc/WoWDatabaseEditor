using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonNpcSpellClickSpell : INpcSpellClickSpell
{
    public uint CreatureId { get; set; }
    public uint SpellId { get; set; }
    public NpcSpellClickFlags CastFlags { get; set; }
}

public class JsonMangosCreatureEquipmentTemplate : IMangosCreatureEquipmentTemplate
{
    public uint Item1 { get; set; }
    public uint Item2 { get; set; }
    public uint Item3 { get; set; }
    public uint GetItem(int id)
    {
        return id switch
        {
            1 => Item1,
            2 => Item2,
            3 => Item3,
            _ => 0
        };
    }

    public uint Entry { get; set; }
}
