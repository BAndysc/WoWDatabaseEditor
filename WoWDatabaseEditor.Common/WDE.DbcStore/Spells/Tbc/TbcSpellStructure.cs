using System;
using WDE.Common.Services;

namespace WDE.DbcStore.Spells.Tbc
{
    public class TbcSpellCastTime
    {
        public uint Id;
        public uint BaseTimeMs;
        public uint PerLevelMs;
        public uint MinimumMs;
    }
    
    public struct TbcSpellStructure
    {
        public uint Id;
        public uint Category;
        public uint CastUi;
        public uint DispelType;
        public uint Mechanic;
        public SpellAttr0 Attributes;
        public SpellAttr1 AttributesEx;
        public SpellAttr2 AttributesExB;
        public SpellAttr3 AttributesExC;
        public SpellAttr4 AttributesExD;
        public SpellAttr5 AttributesExE;
        public SpellAttr6 AttributesExF;
        public uint ShapeshiftMask;
        public uint ShapeshiftExclude;
        public uint Targets;
        public uint TargetCreatureType;
        public uint RequiresSpellFocus;
        public uint FacingCasterFlags;
        public uint CasterAuraSpell;
        public uint TargetAuraSpell;
        public uint ExcludeCasterAuraSpell;
        public uint ExcludeTargetAuraSpell;
        public uint CastingTimeIndex;
        public uint RecoveryTime;
        public uint CategoryRecoveryTime;
        public uint InterruptFlags;
        public uint AuraInterruptFlags;
        public uint ChannelInterruptFlags;
        public uint ProcTypeMask;
        public uint ProcChance;
        public uint ProcCharges;
        public uint MaxLevel;
        public uint BaseLevel;
        public uint SpellLevel;
        public uint DurationIndex;
        public uint PowerType;
        public uint ManaCost;
        public uint ManaCostPerLevel;
        public uint ManaPerSecond;
        public uint ManaPerSecondPerLevel;
        public uint RangeIndex;
        public float Speed;
        public uint ModalNextSpell;
        public uint CumulativeAura;
        public FixedUintArray2 Totem;
        public uint[] Reagent;
        public uint[] ReagentCount;
        public int EquippedItemClass;
        public uint EquippedItemSubclass;
        public uint EquippedItemInvTypes;
        public FixedEffectTypeArray3 Effect;
        public FixedUintArray3 EffectDieSides;
        public FixedUintArray3 EffectBaseDice;
        public FixedUintArray3 EffectDicePerLevel;
        public FixedFloatArray3 EffectRealPointsPerLevel;
        public FixedUintArray3 EffectBasePoints;
        public FixedUintArray3 EffectMechanic;
        public FixedUintArray3 ImplicitTargetA;
        public FixedUintArray3 ImplicitTargetB;
        public FixedUintArray3 EffectRadiusIndex;
        public FixedUintArray3 EffectAura;
        public FixedUintArray3 EffectAmplitude;
        public FixedFloatArray3 EffectMultipleValue;
        public FixedUintArray3 EffectChainTargets;
        public FixedUintArray3 EffectItemType;
        public FixedUintArray3 EffectMiscValue;
        public FixedUintArray3 EffectMiscValueB;
        public FixedUintArray3 EffectTriggerSpell;
        public FixedFloatArray3 EffectPointsPerCombo;
        public FixedUintArray2 SpellVisualId;
        public uint SpellIconId;
        public uint ActiveIconId;
        public uint SpellPriority;
        public string Name;
        public string NameSubtext;
        public string Description;
        public string AuraDescription;
        public uint NameLangMask;
        public uint NameSubtextLangMask;
        public uint DescriptionLangMask;
        public uint AuraDescriptionLangMask;
        public uint ManaCostPct;
        public uint StartRecoveryCategory;
        public uint StartRecoveryTime;
        public uint MaxTargetLevel;
        public uint SpellClassSet;
        public FixedUintArray2 SpellClassMask;
        public uint MaxTargets;
        public uint DefenseType;
        public uint PreventionType;
        public uint StanceBarOrder;
        public FixedFloatArray3 EffectChainAmplitude;
        public uint MinFactionId;
        public uint MinReputation;
        public uint RequiredAuraVision;
        public FixedUintArray2 RequiredTotemCategoryID;
        public uint RequiredAreasId;
        public uint SchoolMask;
        public uint RuneCostId;
        public uint SpellMissileId;
        public uint PowerDisplayId;
        public FixedFloatArray3 EffectBonusCoefficient;
        public uint DescriptionVariablesId;
        public uint Difficulty;
        
        public uint? SkillLine { get; set; }
        public TbcSpellCastTime? CastingTime { get; set; }
    }
}