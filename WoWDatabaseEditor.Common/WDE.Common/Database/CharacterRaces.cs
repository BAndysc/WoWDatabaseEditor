using System;

namespace WDE.Common.Database;

[Flags]
public enum CharacterRaces : uint
{
    None = 0,
    Human = 1,
    Orc = 2,
    Dwarf = 4,
    NightElf = 8,
    Undead = 16,
    Tauren = 32,
    Gnome = 64,
    Troll = 128,
    Goblin = 256,
    BloodElf = 512,
    Draenei = 1024,
    Worgen = 2097152,
    Pandaren = 8388608,
    PandarenAlliance = 16777216,
    PandarenHorde = 33554432,
    Nightborne = 67108864,
    HighmountainTauren = 134217728,
    VoidElf = 268435456,
    LightforgedDraenei = 536870912,
    ZandalariTroll = 1073741824,
    KulTiran = 2147483648,
    DarkIronDwarf = 2048,
    Vulpera = 4096,
    MagharOrc = 8192,
    Mechagnome = 16384,
    DracthyrAlliance = 65536,
    DracthyrHorde = 32768,
    All = Human | Orc | Dwarf | NightElf | Undead | Tauren | Gnome | Troll | Goblin | BloodElf | Draenei |
          Worgen | Pandaren | PandarenAlliance | PandarenHorde | Nightborne | HighmountainTauren | VoidElf | LightforgedDraenei | 
          ZandalariTroll | KulTiran | DarkIronDwarf | Vulpera | MagharOrc | Mechagnome | DracthyrAlliance | DracthyrHorde,
    AllHorde = Orc | Undead | Tauren | Troll | Goblin | BloodElf | PandarenHorde | Nightborne | HighmountainTauren | ZandalariTroll | Vulpera | MagharOrc | DracthyrHorde,
    AllAlliance = Human | Dwarf | NightElf | Gnome | Draenei | Worgen | PandarenAlliance | VoidElf | LightforgedDraenei | KulTiran | DarkIronDwarf | Mechagnome | DracthyrAlliance,
    AllVanilla = Human | Orc | Dwarf | NightElf | Undead | Tauren | Gnome | Troll,
    AllTbc = AllVanilla | Draenei | BloodElf,
    AllWrath = AllTbc,
    AllCatataclysm = AllWrath | Worgen | Goblin,
    AllMoP = AllCatataclysm | Pandaren | PandarenAlliance | PandarenHorde,
    AllWoD = AllMoP,
    AllLegion = AllWoD | Nightborne | HighmountainTauren | VoidElf | LightforgedDraenei,
    AllBfA = AllLegion | ZandalariTroll | KulTiran | DarkIronDwarf | Vulpera | MagharOrc | Mechagnome,
    AllShadowlands = AllBfA,
    AllDragonflight = AllShadowlands | DracthyrAlliance | DracthyrHorde,
}