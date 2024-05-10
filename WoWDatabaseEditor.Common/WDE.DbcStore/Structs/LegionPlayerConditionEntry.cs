using System;
using WDE.Common.DBC.Structs;

namespace WDE.DbcStore.Structs;

public sealed class LegionPlayerConditionEntry : IPlayerCondition
{
    public int Id { get; init; }
    public long RaceMask { get; init; }
    public string? Failure_description_lang { get; init; }
    public byte Flags { get; init; }
    public ushort MinLevel { get; init; }
    public ushort MaxLevel { get; init; }
    public int ClassMask { get; init; }
    public sbyte Gender { get; init; }
    public sbyte NativeGender { get; init; }
    public uint SkillLogic { get; init; }
    public byte LanguageID { get; init; }
    public byte MinLanguage { get; init; }
    public int MaxLanguage { get; init; }
    public ushort MaxFactionID { get; init; }
    public byte MaxReputation { get; init; }
    public uint ReputationLogic { get; init; }
    public sbyte CurrentPvpFaction { get; init; }
    public byte MinPVPRank { get; init; }
    public byte MaxPVPRank { get; init; }
    public byte PvpMedal { get; init; }
    public uint PrevQuestLogic { get; init; }
    public uint CurrQuestLogic { get; init; }
    public uint CurrentCompletedQuestLogic { get; init; }
    public uint SpellLogic { get; init; }
    public uint ItemLogic { get; init; }
    public byte ItemFlags { get; init; }
    public uint AuraSpellLogic { get; init; }
    public ushort WorldStateExpressionID { get; init; }
    public byte WeatherID { get; init; }
    public byte PartyStatus { get; init; }
    public byte LifetimeMaxPVPRank { get; init; }
    public uint AchievementLogic { get; init; }
    public uint LfgLogic { get; init; }
    public uint AreaLogic { get; init; }
    public uint CurrencyLogic { get; init; }
    public ushort QuestKillID { get; init; }
    public uint QuestKillLogic { get; init; }
    public sbyte MinExpansionLevel { get; init; }
    public sbyte MaxExpansionLevel { get; init; }
    public sbyte MinExpansionTier { get; init; }
    public sbyte MaxExpansionTier { get; init; }
    public byte MinGuildLevel { get; init; }
    public byte MaxGuildLevel { get; init; }
    public byte PhaseUseFlags { get; init; }
    public ushort PhaseID { get; init; }
    public uint PhaseGroupID { get; init; }
    public int MinAvgItemLevel { get; init; }
    public int MaxAvgItemLevel { get; init; }
    public ushort MinAvgEquippedItemLevel { get; init; }
    public ushort MaxAvgEquippedItemLevel { get; init; }
    public sbyte ChrSpecializationIndex { get; init; }
    public sbyte ChrSpecializationRole { get; init; }
    public sbyte PowerType { get; init; }
    public byte PowerTypeComp { get; init; }
    public byte PowerTypeValue { get; init; }
    public uint ModifierTreeID { get; init; }
    public int WeaponSubclassMask { get; init; }
    public int SkillIDCount => 4;
    public ushort SkillID0 { get; init; }
    public ushort SkillID1 { get; init; }
    public ushort SkillID2 { get; init; }
    public ushort SkillID3 { get; init; }
    public ushort GetSkillID(int index)
    {
        return index switch
        {
            0 => SkillID0,
            1 => SkillID1,
            2 => SkillID2,
            3 => SkillID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int MinSkillCount => 4;
    public ushort MinSkill0 { get; init; }
    public ushort MinSkill1 { get; init; }
    public ushort MinSkill2 { get; init; }
    public ushort MinSkill3 { get; init; }
    public ushort GetMinSkill(int index)
    {
        return index switch
        {
            0 => MinSkill0,
            1 => MinSkill1,
            2 => MinSkill2,
            3 => MinSkill3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int MaxSkillCount => 4;
    public ushort MaxSkill0 { get; init; }
    public ushort MaxSkill1 { get; init; }
    public ushort MaxSkill2 { get; init; }
    public ushort MaxSkill3 { get; init; }
    public ushort GetMaxSkill(int index)
    {
        return index switch
        {
            0 => MaxSkill0,
            1 => MaxSkill1,
            2 => MaxSkill2,
            3 => MaxSkill3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int MinFactionIDCount => 3;
    public uint MinFactionID0 { get; init; }
    public uint MinFactionID1 { get; init; }
    public uint MinFactionID2 { get; init; }
    public uint GetMinFactionID(int index)
    {
        return index switch
        {
            0 => MinFactionID0,
            1 => MinFactionID1,
            2 => MinFactionID2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int MinReputationCount => 3;
    public byte MinReputation0 { get; init; }
    public byte MinReputation1 { get; init; }
    public byte MinReputation2 { get; init; }
    public byte GetMinReputation(int index)
    {
        return index switch
        {
            0 => MinReputation0,
            1 => MinReputation1,
            2 => MinReputation2,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int PrevQuestIDCount => 4;
    public ushort PrevQuestID0 { get; init; }
    public ushort PrevQuestID1 { get; init; }
    public ushort PrevQuestID2 { get; init; }
    public ushort PrevQuestID3 { get; init; }
    public ushort GetPrevQuestID(int index)
    {
        return index switch
        {
            0 => PrevQuestID0,
            1 => PrevQuestID1,
            2 => PrevQuestID2,
            3 => PrevQuestID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int CurrQuestIDCount => 4;
    public ushort CurrQuestID0 { get; init; }
    public ushort CurrQuestID1 { get; init; }
    public ushort CurrQuestID2 { get; init; }
    public ushort CurrQuestID3 { get; init; }
    public ushort GetCurrQuestID(int index)
    {
        return index switch
        {
            0 => CurrQuestID0,
            1 => CurrQuestID1,
            2 => CurrQuestID2,
            3 => CurrQuestID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int CurrentCompletedQuestIDCount => 4;
    public ushort CurrentCompletedQuestID0 { get; init; }
    public ushort CurrentCompletedQuestID1 { get; init; }
    public ushort CurrentCompletedQuestID2 { get; init; }
    public ushort CurrentCompletedQuestID3 { get; init; }
    public ushort GetCurrentCompletedQuestID(int index)
    {
        return index switch
        {
            0 => CurrentCompletedQuestID0,
            1 => CurrentCompletedQuestID1,
            2 => CurrentCompletedQuestID2,
            3 => CurrentCompletedQuestID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int SpellIDCount => 4;
    public int SpellID0 { get; init; }
    public int SpellID1 { get; init; }
    public int SpellID2 { get; init; }
    public int SpellID3 { get; init; }
    public int GetSpellID(int index)
    {
        return index switch
        {
            0 => SpellID0,
            1 => SpellID1,
            2 => SpellID2,
            3 => SpellID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int ItemIDCount => 4;
    public int ItemID0 { get; init; }
    public int ItemID1 { get; init; }
    public int ItemID2 { get; init; }
    public int ItemID3 { get; init; }
    public int GetItemID(int index)
    {
        return index switch
        {
            0 => ItemID0,
            1 => ItemID1,
            2 => ItemID2,
            3 => ItemID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int ItemCountCount => 4;
    public uint ItemCount0 { get; init; }
    public uint ItemCount1 { get; init; }
    public uint ItemCount2 { get; init; }
    public uint ItemCount3 { get; init; }
    public uint GetItemCount(int index)
    {
        return index switch
        {
            0 => ItemCount0,
            1 => ItemCount1,
            2 => ItemCount2,
            3 => ItemCount3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int ExploredCount => 2;
    public ushort Explored0 { get; init; }
    public ushort Explored1 { get; init; }
    public ushort GetExplored(int index)
    {
        return index switch
        {
            0 => Explored0,
            1 => Explored1,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int TimeCount => 2;
    public uint Time0 { get; init; }
    public uint Time1 { get; init; }
    public uint GetTime(int index)
    {
        return index switch
        {
            0 => Time0,
            1 => Time1,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int AuraSpellIDCount => 4;
    public int AuraSpellID0 { get; init; }
    public int AuraSpellID1 { get; init; }
    public int AuraSpellID2 { get; init; }
    public int AuraSpellID3 { get; init; }
    public int GetAuraSpellID(int index)
    {
        return index switch
        {
            0 => AuraSpellID0,
            1 => AuraSpellID1,
            2 => AuraSpellID2,
            3 => AuraSpellID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int AuraStacksCount => 4;
    public byte AuraStacks0 { get; init; }
    public byte AuraStacks1 { get; init; }
    public byte AuraStacks2 { get; init; }
    public byte AuraStacks3 { get; init; }
    public byte GetAuraStacks(int index)
    {
        return index switch
        {
            0 => AuraStacks0,
            1 => AuraStacks1,
            2 => AuraStacks2,
            3 => AuraStacks3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int AchievementCount => 4;
    public ushort Achievement0 { get; init; }
    public ushort Achievement1 { get; init; }
    public ushort Achievement2 { get; init; }
    public ushort Achievement3 { get; init; }
    public ushort GetAchievement(int index)
    {
        return index switch
        {
            0 => Achievement0,
            1 => Achievement1,
            2 => Achievement2,
            3 => Achievement3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int LfgStatusCount => 4;
    public byte LfgStatus0 { get; init; }
    public byte LfgStatus1 { get; init; }
    public byte LfgStatus2 { get; init; }
    public byte LfgStatus3 { get; init; }
    public byte GetLfgStatus(int index)
    {
        return index switch
        {
            0 => LfgStatus0,
            1 => LfgStatus1,
            2 => LfgStatus2,
            3 => LfgStatus3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int LfgCompareCount => 4;
    public byte LfgCompare0 { get; init; }
    public byte LfgCompare1 { get; init; }
    public byte LfgCompare2 { get; init; }
    public byte LfgCompare3 { get; init; }
    public byte GetLfgCompare(int index)
    {
        return index switch
        {
            0 => LfgCompare0,
            1 => LfgCompare1,
            2 => LfgCompare2,
            3 => LfgCompare3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int LfgValueCount => 4;
    public uint LfgValue0 { get; init; }
    public uint LfgValue1 { get; init; }
    public uint LfgValue2 { get; init; }
    public uint LfgValue3 { get; init; }
    public uint GetLfgValue(int index)
    {
        return index switch
        {
            0 => LfgValue0,
            1 => LfgValue1,
            2 => LfgValue2,
            3 => LfgValue3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int AreaIDCount => 4;
    public ushort AreaID0 { get; init; }
    public ushort AreaID1 { get; init; }
    public ushort AreaID2 { get; init; }
    public ushort AreaID3 { get; init; }
    public ushort GetAreaID(int index)
    {
        return index switch
        {
            0 => AreaID0,
            1 => AreaID1,
            2 => AreaID2,
            3 => AreaID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int CurrencyIDCount => 4;
    public uint CurrencyID0 { get; init; }
    public uint CurrencyID1 { get; init; }
    public uint CurrencyID2 { get; init; }
    public uint CurrencyID3 { get; init; }
    public uint GetCurrencyID(int index)
    {
        return index switch
        {
            0 => CurrencyID0,
            1 => CurrencyID1,
            2 => CurrencyID2,
            3 => CurrencyID3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int CurrencyCountCount => 4;
    public uint CurrencyCount0 { get; init; }
    public uint CurrencyCount1 { get; init; }
    public uint CurrencyCount2 { get; init; }
    public uint CurrencyCount3 { get; init; }
    public uint GetCurrencyCount(int index)
    {
        return index switch
        {
            0 => CurrencyCount0,
            1 => CurrencyCount1,
            2 => CurrencyCount2,
            3 => CurrencyCount3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int QuestKillMonsterCount => 6;
    public uint QuestKillMonster0 { get; init; }
    public uint QuestKillMonster1 { get; init; }
    public uint QuestKillMonster2 { get; init; }
    public uint QuestKillMonster3 { get; init; }
    public uint QuestKillMonster4 { get; init; }
    public uint QuestKillMonster5 { get; init; }
    public uint GetQuestKillMonster(int index)
    {
        return index switch
        {
            0 => QuestKillMonster0,
            1 => QuestKillMonster1,
            2 => QuestKillMonster2,
            3 => QuestKillMonster3,
            4 => QuestKillMonster4,
            5 => QuestKillMonster5,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public int MovementFlagsCount => 2;
    public int MovementFlags0 { get; init; }
    public int MovementFlags1 { get; init; }
    public int GetMovementFlags(int index)
    {
        return index switch
        {
            0 => MovementFlags0,
            1 => MovementFlags1,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

}