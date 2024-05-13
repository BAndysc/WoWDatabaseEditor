using System;
using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models.Wrath
{
    /// <summary>
    /// Creature System
    /// </summary>
    [Table(Name = "creature_template")]
    public class CreatureTemplateWoTLK : ICreatureTemplate
    {
        [Identity]
        [Column("Entry"                 , IsPrimaryKey = true )] public uint    Entry                  { get; set; } // mediumint(8) unsigned
        [Column("Name"                  , CanBeNull    = false)] public string  Name                   { get; set; } = null!; // char(100)
        [Column("SubName"                                     )] public string? SubName                { get; set; } // char(100)
        [Column("IconName"                                    )] public string? IconName               { get; set; } // char(100)
        public uint FactionTemplate => Faction;
        [Column("MinLevel"                                    )] public short   MinLevel               { get; set; } // tinyint(3) unsigned
        [Column("MaxLevel"                                    )] public short   MaxLevel               { get; set; } // tinyint(3) unsigned
        [Column("DifficultyEntry1"                            )] public uint    DifficultyEntry1       { get; set; } // mediumint(8) unsigned
        [Column("DifficultyEntry2"                            )] public uint    DifficultyEntry2       { get; set; } // mediumint(8) unsigned
        [Column("DifficultyEntry3"                            )] public uint    DifficultyEntry3       { get; set; } // mediumint(8) unsigned
        [Column("DisplayId1"                                    )] public uint    ModelId1               { get; set; } // mediumint(8) unsigned
        [Column("DisplayId2"                                    )] public uint    ModelId2               { get; set; } // mediumint(8) unsigned
        [Column("DisplayId3"                                    )] public uint    ModelId3               { get; set; } // mediumint(8) unsigned
        [Column("DisplayId4"                                    )] public uint    ModelId4               { get; set; } // mediumint(8) unsigned
        [Column("Faction"                                     )] public ushort  Faction                { get; set; } // smallint(5) unsigned
        [Column("Scale"                                       )] public float   Scale                  { get; set; } // float
        [Column("Family"                                      )] public int   Family                  { get; set; } // tinyint(4)
        [Column("CreatureType"                                )] public byte    Type                  { get; set; } // tinyint(3) unsigned
        [Column("InhabitType"                                 )] public GameDefines.InhabitType InhabitType { get; set; } // tinyint(3) unsigned
        [Column("RegenerateStats"                             )] public byte    RegenerateStats        { get; set; } // tinyint(3) unsigned
        [Column("RacialLeader"                                )] public bool    RacialLeader           { get; set; } // tinyint(3) unsigned
        [Column("NpcFlags"                                    )] public GameDefines.NpcFlags NpcFlags  { get; set; } // int(10) unsigned
        [Column("UnitFlags"                                   )] public GameDefines.UnitFlags UnitFlags { get; set; } // int(10) unsigned
        [Column("UnitFlags2"                                  )] public GameDefines.UnitFlags2  UnitFlags2 { get; set; } // int(10) unsigned
        [Column("DynamicFlags"                                )] public uint    DynamicFlags           { get; set; } // int(10) unsigned
        [Column("ExtraFlags"                                  )] public uint    FlagsExtra             { get; set; } // int(10) unsigned
        [Column("CreatureTypeFlags"                           )] public uint    TypeFlags              { get; set; } // int(10) unsigned
        [Column("SpeedWalk"                                   )] public float   SpeedWalk              { get; set; } // float
        [Column("SpeedRun"                                    )] public float   SpeedRun               { get; set; } // float
        /// <summary>
        /// Detection range for proximity
        /// </summary>
        [Column("Detection"                                   )] public uint    Detection              { get; set; } // int(10) unsigned
        /// <summary>
        /// Range in which creature calls for help?
        /// </summary>
        [Column("CallForHelp"                                 )] public uint    CallForHelp            { get; set; } // int(10) unsigned
        /// <summary>
        /// When exceeded during pursuit creature evades?
        /// </summary>
        [Column("Pursuit"                                     )] public uint    Pursuit                { get; set; } // int(10) unsigned
        /// <summary>
        /// Leash range from combat start position
        /// </summary>
        [Column("Leash"                                       )] public uint    Leash                  { get; set; } // int(10) unsigned
        /// <summary>
        /// Time for refreshing leashing before evade?
        /// </summary>
        [Column("Timeout"                                     )] public uint    Timeout                { get; set; } // int(10) unsigned
        [Column("UnitClass"                                   )] public byte    UnitClass              { get; set; } // tinyint(3) unsigned
        [Column("Rank"                                        )] public byte    Rank                   { get; set; } // tinyint(3) unsigned
        [Column("Expansion"                                   )] public short   RequiredExpansion      { get; set; } // tinyint(3)
        [Column("HealthMultiplier"                            )] public float   HealthMod              { get; set; } // float
        [Column("PowerMultiplier"                             )] public float   ManaMod                { get; set; } // float
        [Column("DamageMultiplier"                            )] public float   DamageMultiplier       { get; set; } // float
        [Column("DamageVariance"                              )] public float   DamageVariance         { get; set; } // float
        [Column("ArmorMultiplier"                             )] public float   ArmorMultiplier        { get; set; } // float
        [Column("ExperienceMultiplier"                        )] public float   ExperienceMultiplier   { get; set; } // float
        [Column("MinLevelHealth"                              )] public uint    MinLevelHealth         { get; set; } // int(10) unsigned
        [Column("MaxLevelHealth"                              )] public uint    MaxLevelHealth         { get; set; } // int(10) unsigned
        [Column("MinLevelMana"                                )] public uint    MinLevelMana           { get; set; } // int(10) unsigned
        [Column("MaxLevelMana"                                )] public uint    MaxLevelMana           { get; set; } // int(10) unsigned
        [Column("MinMeleeDmg"                                 )] public float   MinMeleeDmg            { get; set; } // float
        [Column("MaxMeleeDmg"                                 )] public float   MaxMeleeDmg            { get; set; } // float
        [Column("MinRangedDmg"                                )] public float   MinRangedDmg           { get; set; } // float
        [Column("MaxRangedDmg"                                )] public float   MaxRangedDmg           { get; set; } // float
        [Column("Armor"                                       )] public uint    Armor                  { get; set; } // int(10) unsigned
        [Column("MeleeAttackPower"                            )] public uint    MeleeAttackPower       { get; set; } // int(10) unsigned
        [Column("RangedAttackPower"                           )] public ushort  RangedAttackPower      { get; set; } // smallint(5) unsigned
        [Column("MeleeBaseAttackTime"                         )] public uint    MeleeBaseAttackTime    { get; set; } // int(10) unsigned
        [Column("RangedBaseAttackTime"                        )] public uint    RangedBaseAttackTime   { get; set; } // int(10) unsigned
        [Column("DamageSchool"                                )] public sbyte   DamageSchool           { get; set; } // tinyint(4)
        [Column("MinLootGold"                                 )] public uint    MinLootGold            { get; set; } // mediumint(8) unsigned
        [Column("MaxLootGold"                                 )] public uint    MaxLootGold            { get; set; } // mediumint(8) unsigned
        [Column("LootId"                                      )] public uint    LootId                 { get; set; } // mediumint(8) unsigned
        [Column("PickpocketLootId"                            )] public uint    PickpocketLootId       { get; set; } // mediumint(8) unsigned
        [Column("SkinningLootId"                              )] public uint    SkinningLootId         { get; set; } // mediumint(8) unsigned
        [Column("KillCredit1"                                 )] public uint    KillCredit1            { get; set; } // int(11) unsigned
        [Column("KillCredit2"                                 )] public uint    KillCredit2            { get; set; } // int(11) unsigned
        [Column("QuestItem1"                                  )] public uint    QuestItem1             { get; set; } // int(11) unsigned
        [Column("QuestItem2"                                  )] public uint    QuestItem2             { get; set; } // int(11) unsigned
        [Column("QuestItem3"                                  )] public uint    QuestItem3             { get; set; } // int(11) unsigned
        [Column("QuestItem4"                                  )] public uint    QuestItem4             { get; set; } // int(11) unsigned
        [Column("QuestItem5"                                  )] public uint    QuestItem5             { get; set; } // int(11) unsigned
        [Column("QuestItem6"                                  )] public uint    QuestItem6             { get; set; } // int(11) unsigned
        [Column("MechanicImmuneMask"                          )] public uint    MechanicImmuneMask     { get; set; } // int(10) unsigned
        [Column("SchoolImmuneMask"                            )] public uint    SchoolImmuneMask       { get; set; } // int(10) unsigned
        [Column("ResistanceHoly"                              )] public short   ResistanceHoly         { get; set; } // smallint(5)
        [Column("ResistanceFire"                              )] public short   ResistanceFire         { get; set; } // smallint(5)
        [Column("ResistanceNature"                            )] public short   ResistanceNature       { get; set; } // smallint(5)
        [Column("ResistanceFrost"                             )] public short   ResistanceFrost        { get; set; } // smallint(5)
        [Column("ResistanceShadow"                            )] public short   ResistanceShadow       { get; set; } // smallint(5)
        [Column("ResistanceArcane"                            )] public short   ResistanceArcane       { get; set; } // smallint(5)
        [Column("PetSpellDataId"                              )] public uint    PetSpellDataId         { get; set; } // mediumint(8) unsigned
        [Column("MovementType"                                )] public byte    MovementType           { get; set; } // tinyint(3) unsigned
        [Column("MovementTemplateId"                          )] public uint    MovementId             { get; set; } // int(11) unsigned
        [Column("TrainerType"                                 )] public sbyte   TrainerType            { get; set; } // tinyint(4)
        [Column("TrainerSpell"                                )] public uint    TrainerSpell           { get; set; } // mediumint(8) unsigned
        [Column("TrainerClass"                                )] public byte    TrainerClass           { get; set; } // tinyint(3) unsigned
        [Column("TrainerRace"                                 )] public byte    TrainerRace            { get; set; } // tinyint(3) unsigned
        [Column("TrainerTemplateId"                           )] public uint    TrainerTemplateId      { get; set; } // mediumint(8) unsigned
        [Column("VendorTemplateId"                            )] public uint    VendorTemplateId       { get; set; } // mediumint(8) unsigned
        [Column("EquipmentTemplateId"                         )] public uint?   EquipmentTemplateId    { get; set; } // mediumint(8) unsigned
        [Column("VehicleTemplateId"                           )] public uint    VehicleId      { get; set; } // mediumint(8) unsigned
        [Column("GossipMenuId"                                )] public uint    GossipMenuId           { get; set; } // mediumint(8) unsigned
        [Column("InteractionPauseTimer"                       )] public int     InteractionPauseTimer  { get; set; } // int(10)
        /// <summary>
        /// Time before corpse despawns
        /// </summary>
        [Column("CorpseDecay"                                 )] public uint    CorpseDecay            { get; set; } // int(10) unsigned
        /// <summary>
        /// creature_spell_list_entry
        /// </summary>
        [Column("SpellList"                                   )] public int     SpellList              { get; set; } // int(11)
        [Column("AIName"                , CanBeNull    = false)] public string  AIName                 { get; set; } = null!; // char(64)
        [Column("ScriptName"            , CanBeNull    = false)] public string  ScriptName             { get; set; } = null!; // char(64)
        
        public int ModelsCount => 4;
        public uint GetModel(int index)
        {
            switch (index)
            {
                case 0:
                    return ModelId1;
                case 1:
                    return ModelId2;
                case 2:
                    return ModelId3;
                case 3:
                    return ModelId4;
            }

            throw new Exception("Model out of range");
        }

        public int LootCount => 1;
        
        public uint GetLootId(int index) => LootId;
    }
}