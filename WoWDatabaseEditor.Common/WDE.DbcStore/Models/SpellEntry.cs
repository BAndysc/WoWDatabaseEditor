using System.Runtime.InteropServices;

namespace WDE.DbcStore.Models
{
    public sealed class SpellEntry
    {
        public uint ActiveIconId; // 20       m_activeIconID
        public uint Attributes; // 1        m_attribute
        public uint AttributesEx; // 2        m_attributesEx
        public uint AttributesEx10; // 11       m_attributesExJ
        public uint AttributesEx2; // 3        m_attributesExB
        public uint AttributesEx3; // 4        m_attributesExC
        public uint AttributesEx4; // 5        m_attributesExD
        public uint AttributesEx5; // 6        m_attributesExE
        public uint AttributesEx6; // 7        m_attributesExF
        public uint AttributesEx7; // 8        m_attributesExG
        public uint AttributesEx8; // 9        m_attributesExH
        public uint AttributesEx9; // 10       m_attributesExI
        public uint CastingTimeIndex; // 12       m_castingTimeIndex
        public string Description = ""; // 23       m_description_lang not used
        public uint DurationIndex; // 13       m_durationIndex
        public uint Id; // 0        m_ID
        public uint PowerType; // 14       m_powerType
        public uint RangeIndex; // 15       m_rangeIndex
        public string Rank = ""; // 22       m_nameSubtext_lang
        public uint ResearchProject; // 47       ResearchProject.dbc //  not used
        public uint RuneCostId; // 26       m_runeCostID
        public uint SchoolMask; // 25       m_schoolMask
        public float Speed; // 16       m_speed
        public uint SpellAuraOptionsId; // 32       SpellAuraOptions.dbc
        public uint SpellAuraRestrictionsId; // 33       SpellAuraRestrictions.dbc
        public uint SpellCastingRequirementsId; // 34       SpellCastingRequirements.dbc
        public uint SpellCategoriesId; // 35       SpellCategories.dbc
        public uint SpellClassOptionsId; // 36       SpellClassOptions.dbc
        public uint SpellCooldownsId; // 37       SpellCooldowns.dbc
        public uint SpellDescriptionVariableId; // 28       3.2.0 - SpellDescriptionVariables.dbc
        public uint SpellDifficultyId; // 29       m_spellDifficultyID - id from SpellDifficulty.dbc
        public uint SpellEquippedItemsId; // 39       SpellEquippedItems.dbc
        public uint SpellIconId; // 19       m_spellIconID
        public uint SpellInterruptsId; // 40       SpellInterrupts.dbc
        public uint SpellLevelsId; // 41       SpellLevels.dbc
        public uint SpellMissileId; // 27       m_spellMissileID //  not used
        public string SpellName = ""; // 21       m_name_lang
        public uint SpellPowerId; // 42       SpellPower.dbc
        public uint SpellReagentsId; // 43       SpellReagents.dbc
        public uint SpellScalingId; // 31       SpellScaling.dbc
        public uint SpellShapeshiftId; // 44       SpellShapeshift.dbc
        public uint SpellTargetRestrictionsId; // 45       SpellTargetRestrictions.dbc
        public uint SpellTotemsId; // 46       SpellTotems.dbc

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public uint[] SpellVisual = new uint[0]; // 17-18    m_spellVisualID

        public string ToolTip = ""; // 24       m_auraDescription_lang not used
        public float Unknown3; // 30
        public uint Unknown4; // 38       all zeros... //  not used
    }
}