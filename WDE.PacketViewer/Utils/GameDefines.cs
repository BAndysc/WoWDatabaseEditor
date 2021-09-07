using System;

namespace WDE.PacketViewer.Utils
{
    public class GameDefines
    {
        [Flags]
        public enum UnitFlags : uint
        {
            UnitFlagServerControlled     = 0x00000001,
            UnitFlagNonAttackable        = 0x00000002,
            UnitFlagRemoveClientControl  = 0x00000004,
            UnitFlagPvpAttackable        = 0x00000008,
            UnitFlagRename               = 0x00000010,
            UnitFlagPreparation          = 0x00000020,
            UnitFlagUnk6                 = 0x00000040,
            UnitFlagNotAttackable1       = 0x00000080,
            UnitFlagImmuneToPc           = 0x00000100,
            UnitFlagImmuneToNpc          = 0x00000200,
            UnitFlagLooting              = 0x00000400,
            UnitFlagPetInCombat          = 0x00000800,
            UnitFlagPvp                  = 0x00001000,
            UnitFlagSilenced             = 0x00002000,
            UnitFlagCannotSwim           = 0x00004000,
            UnitFlagUnk15                = 0x00008000,
            UnitFlagUnk16                = 0x00010000,
            UnitFlagPacified             = 0x00020000,
            UnitFlagStunned              = 0x00040000,
            UnitFlagInCombat             = 0x00080000,
            UnitFlagTaxiFlight           = 0x00100000,
            UnitFlagDisarmed             = 0x00200000,
            UnitFlagConfused             = 0x00400000,
            UnitFlagFleeing              = 0x00800000,
            UnitFlagPlayerControlled     = 0x01000000,
            UnitFlagNotSelectable        = 0x02000000,
            UnitFlagSkinnable            = 0x04000000,
            UnitFlagMount                = 0x08000000,
            UnitFlagUnk28                = 0x10000000,
            UnitFlagUnk29                = 0x20000000,
            UnitFlagSheathe              = 0x40000000,
            UnitFlagUnk31                = 0x80000000,
            
            ServerSideControlled = UnitFlagRename | UnitFlagPetInCombat | UnitFlagInCombat,
        }
    }
}