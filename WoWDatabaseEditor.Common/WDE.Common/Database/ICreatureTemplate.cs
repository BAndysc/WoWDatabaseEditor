using System;

namespace WDE.Common.Database
{
    public interface ICreatureTemplate
    {
        uint Entry { get; }
        uint DifficultyEntry1 { get; }
        uint DifficultyEntry2 { get; }
        uint DifficultyEntry3 { get; }
        float Scale { get; }
        uint GossipMenuId { get; }
        uint FactionTemplate { get; }
        short MinLevel { get; }
        short MaxLevel { get; }
        string Name { get; }
        string? SubName { get; }
        string? IconName { get; }
        string AIName { get; }
        string ScriptName { get; }
        GameDefines.UnitFlags UnitFlags { get; }
        GameDefines.UnitFlags2 UnitFlags2 { get; }
        GameDefines.NpcFlags NpcFlags { get; }
        float SpeedWalk { get; }
        float SpeedRun { get; }
        uint? EquipmentTemplateId { get; }
        short RequiredExpansion { get; }
        byte Rank { get; }
        byte UnitClass { get; }
        int Family { get; }
        byte Type { get; }
        uint TypeFlags { get; }
        uint VehicleId { get; }
        float HealthMod { get; }
        float ManaMod { get; }
        bool RacialLeader { get; }
        uint MovementId { get; }
        uint KillCredit1 { get; }
        uint KillCredit2 { get; }
        uint FlagsExtra { get; }
        GameDefines.InhabitType InhabitType { get; }

        int ModelsCount { get; }
        uint GetModel(int index);
        
        int LootCount { get; }
        uint GetLootId(int index);
        
        uint SkinningLootId { get; }
        uint PickpocketLootId { get; }
    }

    public class AbstractCreatureTemplate : ICreatureTemplate
    {
        public uint Entry { get; init; }
        public uint DifficultyEntry1 { get; init; }
        public uint DifficultyEntry2 { get; init; }
        public uint DifficultyEntry3 { get; init; }
        public float Scale { get; init; }
        public uint GossipMenuId { get; init; }
        public uint FactionTemplate { get; init; }
        public short MinLevel { get; init; }
        public short MaxLevel { get; init; }
        public string Name { get; init; } = "";
        public string? SubName { get; init; }
        public string? IconName { get; init; }
        public string AIName { get; init; } = "";
        public string ScriptName { get; init; } = "";
        public GameDefines.UnitFlags UnitFlags { get; init; }
        public GameDefines.UnitFlags2 UnitFlags2 { get; init; }
        public GameDefines.NpcFlags NpcFlags { get; init; }
        public float SpeedWalk { get; init; }
        public float SpeedRun { get; init; }
        public uint? EquipmentTemplateId { get; init; }
        public short RequiredExpansion { get; init; }
        public byte Rank { get; init; }
        public byte UnitClass { get; init; }
        public int Family { get; init; }
        public byte Type { get; init; }
        public uint TypeFlags { get; init; }
        public uint VehicleId { get; init; }
        public float HealthMod { get; init; }
        public float ManaMod { get; init; }
        public bool RacialLeader { get; init; }
        public uint MovementId { get; init; }
        public uint KillCredit1 { get; init; }
        public uint KillCredit2 { get; init; }
        public uint FlagsExtra { get; init; }
        public int ModelsCount { get; init; }
        public GameDefines.InhabitType InhabitType { get; init; }
        public uint GetModel(int index)
        {
            return 0;
        }
        public int LootCount { get; init; }
        public uint GetLootId(int index)
        {
            return 0;
        }
        public uint SkinningLootId { get; init; }
        public uint PickpocketLootId { get; init; }
    }
    
    public class GameDefines
    {
        [Flags]
        public enum InhabitType : uint
        {
            Ground = 1,
            Water = 2,
            Air = 4,
            Root = 8,
        }
        
        [Flags]
        public enum UnitFlags : uint
        {
            ServerControlled     = 0x00000001,
            NonAttackable        = 0x00000002,
            RemoveClientControl  = 0x00000004,
            PvpAttackable        = 0x00000008,
            Rename               = 0x00000010,
            Preparation          = 0x00000020,
            Unk6                 = 0x00000040,
            NotAttackable1       = 0x00000080,
            ImmuneToPc           = 0x00000100,
            ImmuneToNpc          = 0x00000200,
            Looting              = 0x00000400,
            PetInCombat          = 0x00000800,
            Pvp                  = 0x00001000,
            Silenced             = 0x00002000, // UNIT_FLAG_FORCE_NAMEPLATE in 9.x
            CannotSwim           = 0x00004000,
            Swimming             = 0x00008000,
            NonAttackable2       = 0x00010000,
            Pacified             = 0x00020000,
            Stunned              = 0x00040000,
            InCombat             = 0x00080000,
            TaxiFlight           = 0x00100000,
            Disarmed             = 0x00200000,
            Confused             = 0x00400000,
            Fleeing              = 0x00800000,
            PlayerControlled     = 0x01000000,
            Uninteractible       = 0x02000000,
            Skinnable            = 0x04000000,
            Mount                = 0x08000000,
            Unk28                = 0x10000000,
            PreventEmotesFromChatText = 0x20000000,
            Sheathe              = 0x40000000,
            Immune               = 0x80000000,
            
            ServerSideControlled = Rename | PetInCombat | InCombat | PreventEmotesFromChatText,
            AllowedDatabaseFlags = ImmuneToPc | ImmuneToNpc | CannotSwim | Swimming | Uninteractible | Pacified,
        }

        [Flags]
        public enum UnitFlags2 : uint
        {
            FeignDeath                                     = 0x00000001,
            HideBody                                       = 0x00000002,   // TITLE Hide Body DESCRIPTION Hide unit model (show only player equip)
            IgnoreReputation                               = 0x00000004,
            ComprehendLang                                 = 0x00000008,
            MirrorImage                                    = 0x00000010,
            DontFadeIn                                     = 0x00000020,   // TITLE Don't Fade In DESCRIPTION Unit model instantly appears when summoned (does not fade in)
            ForceMovement                                  = 0x00000040,
            DisarmOffhand                                  = 0x00000080,
            DisablePredStats                               = 0x00000100,   // Player has disabled predicted stats (Used by raid frames)
            AllowChangingTalents                           = 0x00000200,   // Allows changing talents outside rest area
            DisarmRanged                                   = 0x00000400,   // this does not disable ranged weapon display (maybe additional flag needed?)
            RegeneratePower                                = 0x00000800,
            RestrictPartyInteraction                       = 0x00001000,   // Restrict interaction to party or raid
            PreventSpellClick                              = 0x00002000,   // Prevent spellclick
            InteractWhileHostile                           = 0x00004000,   // TITLE Interact while Hostile
            CannotTurn                                     = 0x00008000,   // TITLE Cannot Turn
            Unk2                                           = 0x00010000,
            PlayDeathAnim                                  = 0x00020000,   // Plays special death animation upon death
            AllowCheatSpells                               = 0x00040000,   // Allows casting spells with AttributesEx7 & SPELL_ATTR7_IS_CHEAT_SPELL
            SuppressHighlightWhenTargetedOrMousedOver      = 0x00080000,   // TITLE Suppress highlight when targeted or moused over
            TreatAsRaidUnitForHelpfulSpells                = 0x00100000,   // TITLE Treat as Raid Unit For Helpful Spells (Instances ONLY)
            LargeAoi                                       = 0x00200000,   // TITLE Large (AOI)
            GiganticAoi                                    = 0x00400000,   // TITLE Gigantic (AOI)
            NoActions                                      = 0x00800000,
            AiWillOnlySwimIfTargetSwims                    = 0x01000000,   // TITLE AI will only swim if target swims
            DontGenerateCombatLogWhenEngagedWithNpcs       = 0x02000000,   // TITLE Don't generate combat log when engaged with NPC's
            UntargetableByClient                           = 0x04000000,   // TITLE Untargetable By Client
            AttackerIgnoresMinimumRanges                   = 0x08000000,   // TITLE Attacker Ignores Minimum Ranges
            UninteractibleIfHostile                        = 0x10000000,   // TITLE Uninteractible If Hostile
            Unused11                                       = 0x20000000,
            InfiniteAoi                                    = 0x40000000,   // TITLE Infinite (AOI)
            Unused13                                       = 0x80000000,

            DisallowedDatabaseFlags                        = (FeignDeath | IgnoreReputation | ComprehendLang |
                                                               MirrorImage | ForceMovement | DisarmOffhand |
                                                               DisablePredStats | AllowChangingTalents | DisarmRanged |
                                                            /* UNIT_FLAG2_REGENERATE_POWER | */ RestrictPartyInteraction |
                                                               PreventSpellClick | InteractWhileHostile | /* UNIT_FLAG2_UNK2 | */
                                                            /* UNIT_FLAG2_PLAY_DEATH_ANIM | */ AllowCheatSpells | SuppressHighlightWhenTargetedOrMousedOver |
                                                               TreatAsRaidUnitForHelpfulSpells | LargeAoi | GiganticAoi | NoActions |
                                                               AiWillOnlySwimIfTargetSwims | DontGenerateCombatLogWhenEngagedWithNpcs | AttackerIgnoresMinimumRanges |
                                                               UninteractibleIfHostile | Unused11 | InfiniteAoi | Unused13),  // SKIP

            AllowedDatabaseFlags                            = (0xFFFFFFFF & ~DisallowedDatabaseFlags) // SKIP
        }
        
        [Flags]
        public enum NpcFlags : long
        {
            None                  = 0x00000000,
            Gossip                = 0x00000001,
            QuestGiver            = 0x00000002,
            Unk1                  = 0x00000004,
            Unk2                  = 0x00000008,
            Trainer               = 0x00000010,
            TrainerClass         = 0x00000020,
            TrainerProfession    = 0x00000040,
            Vendor                = 0x00000080,
            VendorAmmo           = 0x00000100,
            VendorFood           = 0x00000200,
            VendorPoison         = 0x00000400,
            VendorReagent        = 0x00000800,
            Repair                = 0x00001000,
            FlightMaster          = 0x00002000,
            SpiritHealer          = 0x00004000,
            SpiritGuide           = 0x00008000,
            Innkeeper             = 0x00010000,
            Banker                = 0x00020000,
            Petitioner            = 0x00040000,
            TabardDesigner        = 0x00080000,
            BattleMaster          = 0x00100000,
            Auctioneer            = 0x00200000,
            StableMaster          = 0x00400000,
            GuildBanker          = 0x00800000,
            SpellClick            = 0x01000000,
            PlayerVehicle        = 0x02000000,
            Mailbox               = 0x04000000
        }
        
        [Flags]
        public enum NpcFlags2 : long
        {
            None                        = 0x00000000,
            ItemUpgradeMaster           = 0x00000001,
            GarrisonArchitect           = 0x00000002,
            Steering                    = 0x00000004,
            AreaSpiritHealerIndividual  = 0x00000008,
            ShipmentCrafter             = 0x00000010,
            GarrisonMissionNPC          = 0x00000020,
            TradeSkillNPC               = 0x00000040,
            BlackMarketView             = 0x00000080,
            FollowerRecruiter           = 0x00000100,
            OrderHallTechTree           = 0x00000200,
            ContributionCollector       = 0x00000400,
            ArgusTeleporter             = 0x00000800,
            UIItemInteraction           = 0x00001000,
            AzeriteRespec               = 0x00004000,
            IslandQueue                 = 0x00008000,
            SupressNPCSounds            = 0x00010000,
        }
    }
}