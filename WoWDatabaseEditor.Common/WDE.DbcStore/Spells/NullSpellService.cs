using System;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Spells
{
    public class NullSpellService : IDbcSpellService, IDbcSpellLoader
    {
        public bool Exists(uint spellId) => true;
        
        public int SpellCount => 0;

        public uint GetSpellId(int index) => 0;

        public T GetAttributes<T>(uint spellId) where T : unmanaged, Enum => default;

        public uint? GetSkillLine(uint spellId) => null;

        public uint? GetSpellFocus(uint spellId) => null;
        
        public TimeSpan? GetSpellCastingTime(uint spellId) => null;
        
        public TimeSpan? GetSpellDuration(uint spellId) => null;

        public TimeSpan? GetSpellCategoryRecoveryTime(uint spellId) => null;
        public string GetName(uint spellId) => "Unknown";

        public event Action<ISpellService>? Changed;

        public string? GetDescription(uint spellId) => null;

        public int GetSpellEffectsCount(uint spellId) => 0;
        
        public SpellAuraType GetSpellAuraType(uint spellId, int effectIndex) => SpellAuraType.None;

        public SpellEffectType GetSpellEffectType(uint spellId, int index) => SpellEffectType.None;

        public SpellTargetFlags GetSpellTargetFlags(uint spellId) => SpellTargetFlags.None;

        public (SpellTarget, SpellTarget) GetSpellEffectTargetType(uint spellId, int index) =>
            (SpellTarget.NoTarget, SpellTarget.NoTarget);

        public uint GetSpellEffectMiscValueA(uint spellId, int index) => 0;
        
        public uint GetSpellEffectTriggerSpell(uint spellId, int index) => 0;

        public DBCVersions Version => 0;
        
        public void Load(string path, DBCLocales dbcLocales)
        {
            
        }
    }
}