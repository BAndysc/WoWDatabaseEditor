using System;
using WDE.Common.Services;

namespace WDE.DbcStore.Spells
{
    public class NullSpellService : ISpellService
    {
        public bool Exists(uint spellId) => true;

        public T GetAttributes<T>(uint spellId) where T : Enum => default;

        public uint? GetSkillLine(uint spellId) => null;

        public uint? GetSpellFocus(uint spellId) => null;
        
        public TimeSpan? GetSpellCastingTime(uint spellId) => null;

        public string? GetDescription(uint spellId) => null;

        public int GetSpellEffectsCount(uint spellId) => 0;

        public SpellEffectType GetSpellEffectType(uint spellId, int index) => SpellEffectType.None;

        public (SpellTarget, SpellTarget) GetSpellEffectTargetType(uint spellId, int index) =>
            (SpellTarget.NoTarget, SpellTarget.NoTarget);

        public uint GetSpellEffectMiscValueA(uint spellId, int index) => 0;
        
        public uint GetSpellEffectTriggerSpell(uint spellId, int index) => 0;
    }
}