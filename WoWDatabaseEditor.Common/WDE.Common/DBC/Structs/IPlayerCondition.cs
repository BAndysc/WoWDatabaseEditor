namespace WDE.Common.DBC.Structs;

public interface IPlayerCondition
{
    int Id { get; }
    long RaceMask { get; }
    string? Failure_description_lang { get; }
    byte Flags { get; }
    ushort MinLevel { get; }
    ushort MaxLevel { get; }
    int ClassMask { get; }
    sbyte Gender { get; }
    sbyte NativeGender { get; }
    uint SkillLogic { get; }
    byte LanguageID { get; }
    byte MinLanguage { get; }
    int MaxLanguage { get; }
    ushort MaxFactionID { get; }
    byte MaxReputation { get; }
    uint ReputationLogic { get; }
    sbyte CurrentPvpFaction { get; }
    byte MinPVPRank { get; }
    byte MaxPVPRank { get; }
    byte PvpMedal { get; }
    uint PrevQuestLogic { get; }
    uint CurrQuestLogic { get; }
    uint CurrentCompletedQuestLogic { get; }
    uint SpellLogic { get; }
    uint ItemLogic { get; }
    byte ItemFlags { get; }
    uint AuraSpellLogic { get; }
    ushort WorldStateExpressionID { get; }
    byte WeatherID { get; }
    byte PartyStatus { get; }
    byte LifetimeMaxPVPRank { get; }
    uint AchievementLogic { get; }
    uint LfgLogic { get; }
    uint AreaLogic { get; }
    uint CurrencyLogic { get; }
    ushort QuestKillID { get; }
    uint QuestKillLogic { get; }
    sbyte MinExpansionLevel { get; }
    sbyte MaxExpansionLevel { get; }
    sbyte MinExpansionTier { get; }
    sbyte MaxExpansionTier { get; }
    byte MinGuildLevel { get; }
    byte MaxGuildLevel { get; }
    byte PhaseUseFlags { get; }
    ushort PhaseID { get; }
    uint PhaseGroupID { get; }
    int MinAvgItemLevel { get; }
    int MaxAvgItemLevel { get; }
    ushort MinAvgEquippedItemLevel { get; }
    ushort MaxAvgEquippedItemLevel { get; }
    sbyte ChrSpecializationIndex { get; }
    sbyte ChrSpecializationRole { get; }
    sbyte PowerType { get; }
    byte PowerTypeComp { get; }
    byte PowerTypeValue { get; }
    uint ModifierTreeID { get; }
    int WeaponSubclassMask { get; }
    int SkillIDCount { get; }
    ushort GetSkillID(int index);
    int MinSkillCount { get; }
    ushort GetMinSkill(int index);
    int MaxSkillCount { get; }
    ushort GetMaxSkill(int index);
    int MinFactionIDCount { get; }
    uint GetMinFactionID(int index);
    int MinReputationCount { get; }
    byte GetMinReputation(int index);
    int PrevQuestIDCount { get; }
    ushort GetPrevQuestID(int index);
    int CurrQuestIDCount { get; }
    ushort GetCurrQuestID(int index);
    int CurrentCompletedQuestIDCount { get; }
    ushort GetCurrentCompletedQuestID(int index);
    int SpellIDCount { get; }
    int GetSpellID(int index);
    int ItemIDCount { get; }
    int GetItemID(int index);
    int ItemCountCount { get; }
    uint GetItemCount(int index);
    int ExploredCount { get; }
    ushort GetExplored(int index);
    int TimeCount { get; }
    uint GetTime(int index);
    int AuraSpellIDCount { get; }
    int GetAuraSpellID(int index);
    int AuraStacksCount { get; }
    byte GetAuraStacks(int index);
    int AchievementCount { get; }
    ushort GetAchievement(int index);
    int LfgStatusCount { get; }
    byte GetLfgStatus(int index);
    int LfgCompareCount { get; }
    byte GetLfgCompare(int index);
    int LfgValueCount { get; }
    uint GetLfgValue(int index);
    int AreaIDCount { get; }
    ushort GetAreaID(int index);
    int CurrencyIDCount { get; }
    uint GetCurrencyID(int index);
    int CurrencyCountCount { get; }
    uint GetCurrencyCount(int index);
    int QuestKillMonsterCount { get; }
    uint GetQuestKillMonster(int index);
    int MovementFlagsCount { get; }
    int GetMovementFlags(int index);
}