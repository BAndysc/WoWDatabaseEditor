using System;

namespace WDE.PacketViewer.Utils
{
    public class GameDefines
    {
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
            Silenced             = 0x00002000,
            CannotSwim           = 0x00004000,
            Unk15                = 0x00008000,
            Unk16                = 0x00010000,
            Pacified             = 0x00020000,
            Stunned              = 0x00040000,
            InCombat             = 0x00080000,
            TaxiFlight           = 0x00100000,
            Disarmed             = 0x00200000,
            Confused             = 0x00400000,
            Fleeing              = 0x00800000,
            PlayerControlled     = 0x01000000,
            NotSelectable        = 0x02000000,
            Skinnable            = 0x04000000,
            Mount                = 0x08000000,
            Unk28                = 0x10000000,
            Unk29                = 0x20000000,
            Sheathe              = 0x40000000,
            Unk31                = 0x80000000,
            
            ServerSideControlled = Rename | PetInCombat | InCombat,
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
    }
}