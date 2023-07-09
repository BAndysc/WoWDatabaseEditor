using System;

namespace WDE.Common.Database;

[Flags]
public enum CharacterClasses
{
    None = 0,
    Warrior = 1,
    Paladin = 2,
    Hunter = 4,
    Rogue = 8,
    Priest = 16,
    DeathKnight = 32,
    Shaman = 64,
    Mage = 128,
    Warlock = 256,
    Monk = 512,
    Druid = 1024,
    DemonHunter = 2048,
    All = Warrior | Paladin | Hunter | Rogue | Priest | DeathKnight | Shaman | Mage | Warlock | Monk | Druid | DemonHunter,
    AllVanilla = Warrior | Paladin | Hunter | Rogue | Priest | Shaman | Mage | Warlock | Druid,
    AllTbc = AllVanilla,
    AllWrath = AllVanilla | DeathKnight,
    AllCataclysm = AllWrath,
    AllMoP = AllCataclysm | Monk,
    AllWoD = AllMoP,
    AllLegion = AllWoD | DemonHunter,
    AllBfA = AllLegion,
    AllShadowlands = AllBfA,
    AllDragonflight = AllShadowlands,
}