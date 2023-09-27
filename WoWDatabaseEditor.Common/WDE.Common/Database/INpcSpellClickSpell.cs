using System;

namespace WDE.Common.Database;

public interface INpcSpellClickSpell
{
    uint CreatureId { get; }
    uint SpellId { get; }
    NpcSpellClickFlags CastFlags { get; }
}

[Flags]
public enum NpcSpellClickFlags
{
    CasterClickee = 0,
    CasterClicker = 1,
    TargetClickee = 0,
    TargetClicker = 2
}