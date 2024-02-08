using System;
using System.Collections.Generic;
using System.IO;
using WDE.Common.Services;
using WDE.DbcStore.FastReader;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Spells.Legion;

public class LegionSpellService : IDbcSpellService, IDbcSpellLoader
{
    private readonly DatabaseClientFileOpener opener;
    private readonly Dictionary<uint, SpellData> spells = new();
    private readonly List<SpellData> spellsList = new();

    private struct SpellCastTime
    {
        public uint BaseTimeMs;
        public uint PerLevelMs;
        public uint MinimumMs;
    }
    
    private struct SpellDuration
    {
        public uint Duration;
        public uint MaxDuration;
        public uint DurationPerLevel;
    }

    private struct SpellCooldown
    {
        public uint CategoryRecoveryTime;
        public uint RecoveryTime;
        public uint StartRecoveryTime;
        public uint DifficultyID;
    }

    private struct SpellEffect
    {
        public SpellEffectType Type;
        public SpellAuraType AuraType;
        public SpellTarget TargetA;
        public SpellTarget TargetB;
        public int MiscValueA;
        public int MiscValueB;
        public uint TriggerSpell;
    }
    
    private class SpellData
    {
        public uint Id;
        public uint[] Attributes = new uint[14];
        public string Name = "";
        public SpellCastTime? CastTime;
        public SpellDuration? Duration;
        public SpellCooldown? Cooldown;
        public SpellTargetFlags SpellTargetFlags;
        public SpellEffect[]? Effects;
    }

    public LegionSpellService(DatabaseClientFileOpener opener)
    {
        this.opener = opener;
    }

    public void Load(string path, DBCLocales dbcLocales)
    {
        Dictionary<uint, SpellCastTime> spellCastTimes = new();
        Dictionary<uint, SpellDuration> spellDuration = new();
        Dictionary<uint, SpellCooldown> spellCooldowns = new();
        foreach (var row in opener.Open(Path.Join(path, "SpellCastTimes.db2")))
        {
            var id = row.GetUInt(0);
            var baseTimeMs = row.GetUInt(1);
            var perLevelMs = row.GetUInt(2);
            var minimumMs = row.GetUInt(3);
                
            spellCastTimes[id] = new SpellCastTime()
            {
                BaseTimeMs = baseTimeMs,
                PerLevelMs = perLevelMs,
                MinimumMs = minimumMs
            };
        }
        
        foreach (var row in opener.Open(Path.Join(path, "SpellDuration.db2")))
        {
            var id = row.GetUInt(0);
            var duration = row.GetUInt(1);
            var maxDuration = row.GetUInt(2);
            var perLevelDuration = row.GetUInt(3);
                
            spellDuration[id] = new SpellDuration()
            {
                Duration = duration,
                MaxDuration = maxDuration,
                DurationPerLevel = perLevelDuration
            };
        }
        
        foreach (var row in opener.Open(Path.Join(path, "SpellMisc.db2")))
        {
            var id = row.GetUInt(0);
            var castingTimeIndex = row.GetUShort(1);
            var durationIndex = row.GetUShort(2);
            var rangeIndex = row.GetUShort(3);
            var spellId = row.GetUInt(11);

            var spell = new SpellData() { Id = spellId };
            
            for (int i = 0; i < 14; ++i)
            {
                var attributes = row.GetUInt(10, i);
                spell.Attributes[i] = attributes;
            }

            if (spellCastTimes.TryGetValue(castingTimeIndex, out var castTime))
                spell.CastTime = castTime;
            
            if (spellDuration.TryGetValue(durationIndex, out var duration))
                spell.Duration = duration;
            
            spells[spellId] = spell;
        }
        
        foreach (var row in opener.Open(Path.Join(path, "SpellCooldowns.db2")))
        {
            var id = row.GetUInt(0);
            var categoryRecoveryTime = row.GetUInt(1);
            var recoveryTime = row.GetUInt(2);
            var startRecoveryTime = row.GetUInt(3);
            var difficultyID = row.GetUInt(4);
            var spellID = row.GetUInt(5);

            if (spells.TryGetValue(spellID, out var spell))
                spell.Cooldown = new SpellCooldown()
                {
                    CategoryRecoveryTime = categoryRecoveryTime,
                    RecoveryTime = recoveryTime,
                    StartRecoveryTime = startRecoveryTime,
                    DifficultyID = difficultyID
                };
        }
        
        foreach (var row in opener.Open(Path.Join(path, "SpellTargetRestrictions.db2")))
        {
            var id = row.GetUInt(0);
            var targets = row.GetUInt(3);
            var spellID = row.GetUInt(8);

            if (spells.TryGetValue(spellID, out var spell))
                spell.SpellTargetFlags = (SpellTargetFlags)targets;
        }
        
        foreach (var row in opener.Open(Path.Join(path, "SpellEffect.db2")))
        {
            var id = row.GetUInt(0);
            var effectType = row.GetUInt(1);
            var effectIndex = row.GetUInt(3);
            var auraType = row.GetUInt(4);
            var targetA = row.GetUInt(28, 0);
            var targetB = row.GetUInt(28, 1);
            var spellId = row.GetUInt(29);
            var miscValueA = row.GetInt(26, 0);
            var miscValueB = row.GetInt(26, 1);
            var triggerSpell = row.GetUInt(16);

            if (spells.TryGetValue(spellId, out var spell))
            {
                var effect = new SpellEffect()
                {
                    Type = (SpellEffectType)effectType,
                    AuraType = (SpellAuraType)auraType,
                    TargetA = (SpellTarget)targetA,
                    TargetB = (SpellTarget)targetB,
                    MiscValueA = miscValueA,
                    MiscValueB = miscValueB,
                    TriggerSpell = triggerSpell
                };
                if (spell.Effects == null || spell.Effects.Length <= effectIndex)
                    Array.Resize(ref spell.Effects, (int)effectIndex + 1);
                spell.Effects[effectIndex] = effect;
            }
        }

        foreach (var row in opener.Open(Path.Join(path, "Spell.db2")))
        {
            var id = row.GetUInt(0);
            var name = row.GetString(1);

            if (!spells.TryGetValue(id, out var spellData))
                spells[id] = spellData = new SpellData() { Id = id };
            spellData.Name = name;
            spellsList.Add(spellData);
        }
            
        Changed?.Invoke(this);
    }

    public bool Exists(uint spellId) => spells.ContainsKey(spellId);

    public int SpellCount => spellsList.Count;

    public uint GetSpellId(int index) => spellsList[index].Id;

    public T GetAttributes<T>(uint spellId) where T : unmanaged, Enum
    {
        if (!spells.TryGetValue(spellId, out var spell))
            return default;

        // kinda unnecessary boxing :/
        if (typeof(T) == typeof(SpellAttr0))
            return (T)(object)spell.Attributes[0];

        if (typeof(T) == typeof(SpellAttr1))
            return (T)(object)spell.Attributes[1];

        if (typeof(T) == typeof(SpellAttr2))
            return (T)(object)spell.Attributes[2];

        if (typeof(T) == typeof(SpellAttr3))
            return (T)(object)spell.Attributes[3];

        if (typeof(T) == typeof(SpellAttr4))
            return (T)(object)spell.Attributes[4];

        if (typeof(T) == typeof(SpellAttr5))
            return (T)(object)spell.Attributes[5];

        if (typeof(T) == typeof(SpellAttr6))
            return (T)(object)spell.Attributes[6];

        if (typeof(T) == typeof(SpellAttr7))
            return (T)(object)spell.Attributes[7];

        if (typeof(T) == typeof(SpellAttr8))
            return (T)(object)spell.Attributes[8];

        if (typeof(T) == typeof(SpellAttr9))
            return (T)(object)spell.Attributes[9];

        if (typeof(T) == typeof(SpellAttr10))
            return (T)(object)spell.Attributes[10];
            
        if (typeof(T) == typeof(SpellAttr11))
            return (T)(object)spell.Attributes[11];

        if (typeof(T) == typeof(SpellAttr12))
            return (T)(object)spell.Attributes[12];

        if (typeof(T) == typeof(SpellAttr13))
            return (T)(object)spell.Attributes[13];
        
        if (typeof(T) == typeof(SpellAttr14))
            return (T)(object)spell.Attributes[14];

        return default;
    }

    public uint? GetSkillLine(uint spellId)
    {
        return default;
    }

    public uint? GetSpellFocus(uint spellId)
    {
        return default;
    }

    public TimeSpan? GetSpellCastingTime(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.CastTime == null ? null : TimeSpan.FromMilliseconds(spell.CastTime.Value.BaseTimeMs);
        return null;
    }

    public TimeSpan? GetSpellDuration(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.Duration == null ? null : TimeSpan.FromMilliseconds(spell.Duration.Value.Duration);
        return null;
    }

    public TimeSpan? GetSpellCategoryRecoveryTime(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.Cooldown == null ? null : TimeSpan.FromMilliseconds(spell.Cooldown.Value.CategoryRecoveryTime);
        return null;
    }

    public string GetName(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.Name;
        return "Unknown";
    }

    public event Action<ISpellService>? Changed;

    public string? GetDescription(uint spellId)
    {
        return default;
    }

    public int GetSpellEffectsCount(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell) && spell.Effects != null)
            return spell.Effects.Length;
        return 0;
    }

    private bool TryGetEffect(uint spellId, int index, out SpellEffect effect)
    {
        effect = default;
        if (spells.TryGetValue(spellId, out var spell) && spell.Effects != null && spell.Effects.Length > index)
        {
            effect = spell.Effects[index];
            return effect.Type != default;
        }
        return false;
    }
    
    public SpellAuraType GetSpellAuraType(uint spellId, int effectIndex)
    {
        if (TryGetEffect(spellId, effectIndex, out var effect))
            return effect.AuraType;
        return default;
    }

    public SpellEffectType GetSpellEffectType(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return effect.Type;
        return default;
    }

    public SpellTargetFlags GetSpellTargetFlags(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.SpellTargetFlags;
        return default;
    }

    public (SpellTarget a, SpellTarget b) GetSpellEffectTargetType(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return (effect.TargetA, effect.TargetB);
        return (SpellTarget.NoTarget, SpellTarget.NoTarget);
    }

    public uint GetSpellEffectMiscValueA(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return (uint)effect.MiscValueA;
        return 0;
    }

    public uint GetSpellEffectTriggerSpell(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return effect.TriggerSpell;
        return 0;
    }

    public DBCVersions Version => DBCVersions.LEGION_26972;
}