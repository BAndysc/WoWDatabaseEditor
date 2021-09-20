using System;
using WDE.Common.Services;

namespace WDE.DbcStore.Spells
{
    public class NullSpellService : ISpellService
    {
        public bool Exists(uint spellId) => true;

        public T GetAttributes<T>(uint spellId) where T : Enum
        {
            return default;
        }

        public uint? GetSkillLine(uint spellId)
        {
            return null;
        }

        public int GetSpellEffectsCount(uint spellId)
        {
            return 0;
        }

        public SpellEffectType GetSpellEffectType(uint spellId, int index)
        {
            return SpellEffectType.None;
        }

        public uint GetSpellEffectMiscValueA(uint spellId, int index)
        {
            return 0;
        }
    }
}