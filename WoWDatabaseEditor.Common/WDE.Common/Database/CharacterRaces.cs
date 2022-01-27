using System;

namespace WDE.Common.Database;

[Flags]
public enum CharacterRaces
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
    All = Human | Orc | Dwarf | NightElf | Undead | Tauren | Gnome | Troll | Goblin | BloodElf | Draenei | Worgen | Pandaren | PandarenAlliance | PandarenHorde,
    AllHorde = Orc | Undead | Tauren | Troll | Goblin | BloodElf | PandarenHorde,
    AllAlliance = Human | Dwarf | NightElf | Gnome | Draenei | Worgen | PandarenAlliance,
    AllVanilla = Human | Orc | Dwarf | NightElf | Undead | Tauren | Gnome | Troll,
    AllTbc = AllVanilla | Draenei | BloodElf,
    AllWrath = AllTbc,
    AllCatataclysm = AllWrath | Worgen | Goblin,
    AllMoP = AllCatataclysm | Pandaren | PandarenAlliance | PandarenHorde,
    AllWoD = AllMoP,
    AllLegion = AllWoD,
    AllBfA = AllLegion,
    AllShadowlands = AllBfA
}