using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WDE.DbcStore.Models
{
    public sealed class SpellEntry
    {
        public uint Id;                                           // 0        m_ID
        public uint Attributes;                                   // 1        m_attribute
        public uint AttributesEx;                                 // 2        m_attributesEx
        public uint AttributesEx2;                                // 3        m_attributesExB
        public uint AttributesEx3;                                // 4        m_attributesExC
        public uint AttributesEx4;                                // 5        m_attributesExD
        public uint AttributesEx5;                                // 6        m_attributesExE
        public uint AttributesEx6;                                // 7        m_attributesExF
        public uint AttributesEx7;                                // 8        m_attributesExG
        public uint AttributesEx8;                                // 9        m_attributesExH
        public uint AttributesEx9;                                // 10       m_attributesExI
        public uint AttributesEx10;                               // 11       m_attributesExJ
        public uint CastingTimeIndex;                             // 12       m_castingTimeIndex
        public uint DurationIndex;                                // 13       m_durationIndex
        public uint PowerType;                                    // 14       m_powerType
        public uint RangeIndex;                                   // 15       m_rangeIndex
        public float Speed;                                       // 16       m_speed
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public uint[] SpellVisual;                                // 17-18    m_spellVisualID
        public uint SpellIconID;                                  // 19       m_spellIconID
        public uint ActiveIconID;                                 // 20       m_activeIconID
        public string SpellName;                                  // 21       m_name_lang
        public string Rank;                                       // 22       m_nameSubtext_lang
        public string Description;                                // 23       m_description_lang not used
        public string ToolTip;                                    // 24       m_auraDescription_lang not used
        public uint SchoolMask;                                   // 25       m_schoolMask
        public uint RuneCostID;                                   // 26       m_runeCostID
        public uint SpellMissileID;                               // 27       m_spellMissileID //  not used
        public uint SpellDescriptionVariableID;                   // 28       3.2.0 - SpellDescriptionVariables.dbc
        public uint SpellDifficultyId;                            // 29       m_spellDifficultyID - id from SpellDifficulty.dbc
        public float Unknown3;                                    // 30
        public uint SpellScalingId;                               // 31       SpellScaling.dbc
        public uint SpellAuraOptionsId;                           // 32       SpellAuraOptions.dbc
        public uint SpellAuraRestrictionsId;                      // 33       SpellAuraRestrictions.dbc
        public uint SpellCastingRequirementsId;                   // 34       SpellCastingRequirements.dbc
        public uint SpellCategoriesId;                            // 35       SpellCategories.dbc
        public uint SpellClassOptionsId;                          // 36       SpellClassOptions.dbc
        public uint SpellCooldownsId;                             // 37       SpellCooldowns.dbc
        public uint Unknown4;                                     // 38       all zeros... //  not used
        public uint SpellEquippedItemsId;                         // 39       SpellEquippedItems.dbc
        public uint SpellInterruptsId;                            // 40       SpellInterrupts.dbc
        public uint SpellLevelsId;                                // 41       SpellLevels.dbc
        public uint SpellPowerId;                                 // 42       SpellPower.dbc
        public uint SpellReagentsId;                              // 43       SpellReagents.dbc
        public uint SpellShapeshiftId;                            // 44       SpellShapeshift.dbc
        public uint SpellTargetRestrictionsId;                    // 45       SpellTargetRestrictions.dbc
        public uint SpellTotemsId;                                // 46       SpellTotems.dbc
        public uint ResearchProject;                              // 47       ResearchProject.dbc //  not used
    }
}
