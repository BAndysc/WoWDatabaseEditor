using System;
using WDE.Common.Services;

namespace WDE.DbcStore.Spells.Wrath
{
    public struct FixedUintArray2
    {
        public uint A { get; set; }
        public uint B { get; set; }
        public uint this[int i]
        {
            get
            {
                if (i < 0 || i >= 2)
                    throw new IndexOutOfRangeException();
                return i == 0 ? A : B;
            }
            set
            {
                if (i < 0 || i >= 2)
                    throw new IndexOutOfRangeException();
                if (i == 0)
                    A = value;
                else
                    B = value;
            }
        }
    }
    
    public struct FixedUintArray3
    {
        public uint A { get; set; }
        public uint B { get; set; }
        public uint C { get; set; }
        public uint this[int i]
        {
            get
            {
                if (i < 0 || i >= 3)
                    throw new IndexOutOfRangeException();
                return i == 0 ? A : (i == 1 ? B : C);
            }
            set
            {
                if (i < 0 || i >= 3)
                    throw new IndexOutOfRangeException();
                if (i == 0)
                    A = value;
                else if (i == 1)
                    B = value;
                else
                    C = value;
            }
        }
    }
    
    
    public struct FixedEffectTypeArray3
    {
        public SpellEffectType A { get; set; }
        public SpellEffectType B { get; set; }
        public SpellEffectType C { get; set; }
        public SpellEffectType this[int i]
        {
            get
            {
                if (i < 0 || i >= 3)
                    throw new IndexOutOfRangeException();
                return i == 0 ? A : (i == 1 ? B : C);
            }
            set
            {
                if (i < 0 || i >= 3)
                    throw new IndexOutOfRangeException();
                if (i == 0)
                    A = value;
                else if (i == 1)
                    B = value;
                else
                    C = value;
            }
        }
    }
    
    public struct FixedFloatArray3
    {
        public float A { get; set; }
        public float B { get; set; }
        public float C { get; set; }
        public float this[int i]
        {
            get
            {
                if (i < 0 || i >= 3)
                    throw new IndexOutOfRangeException();
                return i == 0 ? A : (i == 1 ? B : C);
            }
            set
            {
                if (i < 0 || i >= 3)
                    throw new IndexOutOfRangeException();
                if (i == 0)
                    A = value;
                else if (i == 1)
                    B = value;
                else
                    C = value;
            }
        }
    }

    public class SpellCastTime
    {
        public uint Id;
        public uint BaseTimeMs;
        public uint PerLevelMs;
        public uint MinimumMs;
    }
    
    public class SpellStructure
    {
        public uint Id;
        public uint Category;
        public uint DispelType;
        public uint Mechanic;
        public SpellAttr0 Attributes;
        public SpellAttr1 AttributesEx;
        public SpellAttr2 AttributesExB;
        public SpellAttr3 AttributesExC;
        public SpellAttr4 AttributesExD;
        public SpellAttr5 AttributesExE;
        public SpellAttr6 AttributesExF;
        public SpellAttr7 AttributesExG;
        public FixedUintArray2 ShapeshiftMask;
        public FixedUintArray2 ShapeshiftExclude;
        public uint Targets;
        public uint TargetCreatureType;
        public uint RequiresSpellFocus;
        public uint FacingCasterFlags;
        public uint CasterAuraState;
        public uint TargetAuraState;
        public uint ExcludeCasterAuraState;
        public uint ExcludeTargetAuraState;
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
        //Reagent<32>[8]
        //ReagentCount<32>[8]
        public uint EquippedItemClass;
        public uint EquippedItemSubclass;
        public uint EquippedItemInvTypes;
        public FixedEffectTypeArray3 Effect;
        public FixedUintArray3 EffectDieSides;
        public FixedFloatArray3 EffectRealPointsPerLevel;
        public FixedUintArray3 EffectBasePoints;
        public FixedUintArray3 EffectMechanic;
        public FixedUintArray3 ImplicitTargetA;
        public FixedUintArray3 ImplicitTargetB;
        public FixedUintArray3 EffectRadiusIndex;
        public FixedUintArray3 EffectAura;
        public FixedUintArray3 EffectAuraPeriod;
        public FixedFloatArray3 EffectAmplitude;
        public FixedUintArray3 EffectChainTargets;
        public FixedUintArray3 EffectItemType;
        public FixedUintArray3 EffectMiscValue;
        public FixedUintArray3 EffectMiscValueB;
        public FixedUintArray3 EffectTriggerSpell;
        public FixedFloatArray3 EffectPointsPerCombo;
        public FixedUintArray3 EffectSpellClassMaskA;
        public FixedUintArray3 EffectSpellClassMaskB;
        public FixedUintArray3 EffectSpellClassMaskC;
        public FixedUintArray2 SpellVisualId;
        public uint SpellIconId;
        public uint ActiveIconId;
        public uint SpellPriority;
        public string Name;
        public string NameSubtext;
        public string Description;
        public string AuraDescription;
        public uint ManaCostPct;
        public uint StartRecoveryCategory;
        public uint StartRecoveryTime;
        public uint MaxTargetLevel;
        public uint SpellClassSet;
        public FixedUintArray3 SpellClassMask;
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
        public SpellCastTime? CastingTime { get; set; }
    }
}