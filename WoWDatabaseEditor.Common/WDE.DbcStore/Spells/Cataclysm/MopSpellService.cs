using System;
using System.Collections.Generic;
using System.IO;
using WDE.Common.Services;
using WDE.DbcStore.FastReader;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Spells.Cataclysm;

public class MopSpellService : IDbcSpellService, IDbcSpellLoader
{
    private readonly DatabaseClientFileOpener opener;

    private class SpellCastTime
    {
        public uint Id;
        public uint BaseTimeMs;
        public uint PerLevelMs;
        public uint MinimumMs;
    }
    
    private class SpellEffect
    {
        public uint Id { get; init; }
        public uint DifficultyId { get; init; }
        public SpellEffectType EffectType { get; init; }
        public float EffectAmplitude { get; init; }
        public SpellAuraType AuraType { get; init; }
        public uint AuraPeriod { get; init; }
        public uint EffectBasePoints { get; init; }
        public float EffectBonusCoefficient { get; init; }
        public float EffectChainAmplitude { get; init; }
        public uint EffectChainTargets { get; init; }
        public uint EffectDieSides { get; init; }
        public uint EffectItemType { get; init; }
        public uint EffectMechanic { get; init; }
        public uint EffectMiscValueA { get; init; }
        public uint EffectMiscValueB { get; init; }
        public float EffectPointsPerResource { get; init; }
        public uint EffectRadiusIndexA { get; init; }
        public uint EffectRadiusIndexB { get; init; }
        public float EffectRealPointsPerLevel { get; init; }
        public uint[] EffectSpellClassMask { get; init; } = Array.Empty<uint>();
        public uint EffectTriggerSpell { get; init; }
        public float EffectPosFacing { get; init; }
        public uint[] ImplicitTarget { get; init; } = Array.Empty<uint>();
        public uint SpellId { get; init; }
        public uint EffectIndex { get; init; }
        public uint EffectAttributes { get; init; }
    }

    private class SpellCastingRequirements
    {
        public uint FacingCasterFlags { get; init; }
        public uint MinFactionId { get; init; }
        public uint MinReputation { get; init; }
        public uint RequiredAreasId { get; init; }
        public uint RequiredAuraVision { get; init; }
        public uint RequiresSpellFocus { get; init; }
    }

    private class SpellMiscData
    {
        public uint Id { get; init; }
        public uint SpellId { get; init; }
        public uint DifficultyId { get; init; }
        public SpellAttr0 Attr0 { get; init; }
        public SpellAttr1 Attr1 { get; init; }
        public SpellAttr2 Attr2 { get; init; }
        public SpellAttr3 Attr3 { get; init; }
        public SpellAttr4 Attr4 { get; init; }
        public SpellAttr5 Attr5 { get; init; }
        public SpellAttr6 Attr6 { get; init; }
        public SpellAttr7 Attr7 { get; init; }
        public SpellAttr8 Attr8 { get; init; }
        public SpellAttr9 Attr9 { get; init; }
        public SpellAttr10 Attr10 { get; init; }
        public SpellAttr11 Attr11 { get; init; }
        public SpellAttr12 Attr12 { get; init; }
        public SpellAttr13 Attr13 { get; init; }
        public uint CastingTimeIndex { get; init; }
        public uint DurationIndex { get; init; }
        public uint RangeIndex { get; init; }
        public float Speed { get; init; }
        public uint SpellVisualId1 { get; init; }
        public uint SpellVisualId2 { get; init; }
        public uint SpellIconId { get; init; }
        public uint ActiveIconId { get; init; }
        public uint SchoolMask { get; init; }
    }
    
    private class SpellData
    {
        public uint Id { get; init; }
        public SpellEffect[]? SpellEffects { get; set; }
        public uint SpellCastingRequirementsId { get; init; }
        public SpellCastingRequirements? SpellCastingRequirements { get; init; }

        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public uint? SkillLine { get; set; }
        public SpellCastTime? CastTime { get; set; }
        public SpellMiscData? SpellMisc { get; set; }
    }

    private Dictionary<uint, SpellCastTime> spellCastTimes = new();
    private Dictionary<uint, SpellCastingRequirements> requirements = new();
    private Dictionary<uint, SpellData> spells = new();
    private List<SpellData> spellsList = new();

    public MopSpellService(DatabaseClientFileOpener opener)
    {
        this.opener = opener;
    }
    
    public void Load(string path, DBCLocales dbcLocales)
    {
        foreach (var row in opener.Open(Path.Join(path, "SpellCastingRequirements.dbc")))
        {
            requirements[row.GetUInt(0)] = new SpellCastingRequirements()
            {
                FacingCasterFlags = row.GetUInt(1),
                MinFactionId = row.GetUInt(2),
                MinReputation = row.GetUInt(3),
                RequiredAreasId = row.GetUInt(4),
                RequiredAuraVision = row.GetUInt(5),
                RequiresSpellFocus = row.GetUInt(6)
            };
        }
        
        foreach (var row in opener.Open(Path.Join(path, "SpellCastTimes.dbc")))
        {
            var id = row.GetUInt(0);
            var baseTimeMs = row.GetUInt(1);
            var perLevelMs = row.GetUInt(2);
            var minimumMs = row.GetUInt(3);
            
            spellCastTimes[id] = new SpellCastTime()
            {
                Id = id,
                BaseTimeMs = baseTimeMs,
                PerLevelMs = perLevelMs,
                MinimumMs = minimumMs
            };
        }

        Dictionary<uint, SpellMiscData> spellMiscData = new();
        
        foreach (var row in opener.Open(Path.Join(path, "SpellMisc.dbc")))
        {
            var miscId = row.GetUInt(0);
            spellMiscData.Add(miscId, new SpellMiscData()
            {
                Id = row.GetUInt(0),
                SpellId = row.GetUInt(1),
                DifficultyId = row.GetUInt(2),
                Attr0 = (SpellAttr0)row.GetUInt(3),
                Attr1 = (SpellAttr1)row.GetUInt(4),
                Attr2 = (SpellAttr2)row.GetUInt(5),
                Attr3 = (SpellAttr3)row.GetUInt(6),
                Attr4 = (SpellAttr4)row.GetUInt(7),
                Attr5 = (SpellAttr5)row.GetUInt(8),
                Attr6 = (SpellAttr6)row.GetUInt(9),
                Attr7 = (SpellAttr7)row.GetUInt(10),
                Attr8 = (SpellAttr8)row.GetUInt(11),
                Attr9 = (SpellAttr9)row.GetUInt(12),
                Attr10 = (SpellAttr10)row.GetUInt(13),
                Attr11 = (SpellAttr11)row.GetUInt(14),
                Attr12 = (SpellAttr12)row.GetUInt(15),
                Attr13 = (SpellAttr13)row.GetUInt(16),
                CastingTimeIndex = row.GetUInt(17),
                DurationIndex = row.GetUInt(18),
                RangeIndex = row.GetUInt(19),
                Speed = row.GetFloat(20),
                SpellVisualId1 = row.GetUInt(21),
                SpellVisualId2 = row.GetUInt(22),
                SpellIconId = row.GetUInt(23),
                ActiveIconId = row.GetUInt(24),
                SchoolMask = row.GetUInt(25),
            });
        }

        foreach (var row in opener.Open(Path.Join(path, "Spell.dbc")))
        {
            var id = row.GetUInt(0);
            var miscId = row.GetUInt(24);
            SpellMiscData? spellMisc = null;
            if (miscId > 0)
                spellMiscData.TryGetValue(miscId, out spellMisc);
            
            var castingRequirements = row.GetUInt(12);
            var description = row.GetString(3);

            var spellData = spells[id] = new SpellData()
            {
                Id = id,
                SpellCastingRequirementsId = castingRequirements,
                CastTime = spellMisc?.CastingTimeIndex > 0 && spellCastTimes.TryGetValue(spellMisc.CastingTimeIndex, out var time) ? time : null,
                SpellCastingRequirements = castingRequirements > 0 && requirements.TryGetValue(castingRequirements, out var requirement) ? requirement : null,
                Name = row.GetString(1),
                Description = string.IsNullOrEmpty(description) ? null : description,
                SpellMisc = spellMisc
            };
            spellsList.Add(spellData);
        }

        foreach (var row in opener.Open(Path.Join(path, "SpellEffect.dbc")))
        {
            int i = 0;
            var effect = new SpellEffect()
            {
                Id = row.GetUInt(i++),
                DifficultyId = row.GetUInt(i++),
                EffectType = (SpellEffectType)row.GetUInt(i++),
                EffectAmplitude = row.GetFloat(i++),
                AuraType = (SpellAuraType)row.GetUInt(i++),
                AuraPeriod = row.GetUInt(i++),
                EffectBasePoints = row.GetUInt(i++),
                EffectBonusCoefficient = row.GetFloat(i++),
                EffectChainAmplitude = row.GetFloat(i++),
                EffectChainTargets = row.GetUInt(i++),
                EffectDieSides = row.GetUInt(i++),
                EffectItemType = row.GetUInt(i++),
                EffectMechanic = row.GetUInt(i++),
                EffectMiscValueA = row.GetUInt(i++),
                EffectMiscValueB = row.GetUInt(i++),
                EffectPointsPerResource = row.GetFloat(i++),
                EffectRadiusIndexA = row.GetUInt(i++),
                EffectRadiusIndexB = row.GetUInt(i++),
                EffectRealPointsPerLevel = row.GetFloat(i++),
                EffectSpellClassMask = new[] { row.GetUInt(i++), row.GetUInt(i++), row.GetUInt(i++), row.GetUInt(i++) },
                EffectTriggerSpell = row.GetUInt(i++),
                EffectPosFacing = row.GetFloat(i++),
                ImplicitTarget = new[] { row.GetUInt(i++), row.GetUInt(i++) },
                SpellId = row.GetUInt(i++),
                EffectIndex = row.GetUInt(i++),
                EffectAttributes = row.GetUInt(i++)
            };
            if (!Exists(effect.SpellId))
                continue;
            var spell = spells[effect.SpellId];
            if (spell.SpellEffects == null || spell.SpellEffects.Length >= effect.EffectIndex)
            {
                var array = spell.SpellEffects;
                Array.Resize(ref array, (int)effect.EffectIndex + 1);
                spell.SpellEffects = array;
            }
            spell.SpellEffects[effect.EffectIndex] = effect;
        }


        foreach (var row in opener.Open(Path.Join(path, "SkillLineAbility.dbc")))
        {
            var id = row.GetUInt(0);
            var skillLine = row.GetUInt(1);
            var spellId = row.GetUInt(2);
            
            if (!Exists(spellId))
                continue;
            
            var spell = spells[spellId];
            spell.SkillLine = skillLine;
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

        if (spell.SpellMisc == null)
            return default;
        
        // kinda unnecessary boxing :/
        if (typeof(T) == typeof(SpellAttr0))
            return (T)(object)spell.SpellMisc.Attr0;

        if (typeof(T) == typeof(SpellAttr1))
            return (T)(object)spell.SpellMisc.Attr1;

        if (typeof(T) == typeof(SpellAttr2))
            return (T)(object)spell.SpellMisc.Attr2;

        if (typeof(T) == typeof(SpellAttr3))
            return (T)(object)spell.SpellMisc.Attr3;

        if (typeof(T) == typeof(SpellAttr4))
            return (T)(object)spell.SpellMisc.Attr4;

        if (typeof(T) == typeof(SpellAttr5))
            return (T)(object)spell.SpellMisc.Attr5;

        if (typeof(T) == typeof(SpellAttr6))
            return (T)(object)spell.SpellMisc.Attr6;

        if (typeof(T) == typeof(SpellAttr7))
            return (T)(object)spell.SpellMisc.Attr7;

        if (typeof(T) == typeof(SpellAttr8))
            return (T)(object)spell.SpellMisc.Attr8;

        if (typeof(T) == typeof(SpellAttr9))
            return (T)(object)spell.SpellMisc.Attr9;

        if (typeof(T) == typeof(SpellAttr10))
            return (T)(object)spell.SpellMisc.Attr10;
        
        if (typeof(T) == typeof(SpellAttr11))
            return (T)(object)spell.SpellMisc.Attr11;
        
        if (typeof(T) == typeof(SpellAttr12))
            return (T)(object)spell.SpellMisc.Attr12;
        
        if (typeof(T) == typeof(SpellAttr13))
            return (T)(object)spell.SpellMisc.Attr13;
        
        return default;
    }

    public uint? GetSpellFocus(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.SpellCastingRequirements?.RequiresSpellFocus;
        return null;
    }

    public TimeSpan? GetSpellCastingTime(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.CastTime == null ? null : TimeSpan.FromMilliseconds(spell.CastTime.BaseTimeMs);
        return null;
    }

    public TimeSpan? GetSpellDuration(uint spellId)
    {
        return null;
    }

    public TimeSpan? GetSpellCategoryRecoveryTime(uint spellId)
    {
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
        if (spells.TryGetValue(spellId, out var spell))
            return spell.Description;
        return null;
    }

    public int GetSpellEffectsCount(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.SpellEffects?.Length ?? 0;
        return 0;
    }

    public SpellAuraType GetSpellAuraType(uint spellId, int effectIndex)
    {
        if (TryGetEffect(spellId, effectIndex, out var effect))
            return effect.AuraType;
        return SpellAuraType.None;
    }

    private bool TryGetEffect(uint spellId, int index, out SpellEffect effect)
    {
        effect = null!;
        if (spells.TryGetValue(spellId, out var spell) && spell.SpellEffects != null &&
            spell.SpellEffects.Length > index)
        {
            effect = spell.SpellEffects[index];
            return effect != null;
        }
        return false;
    }
    
    public uint? GetSkillLine(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell.SkillLine;
        return null;
    }
    
    public SpellEffectType GetSpellEffectType(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return effect.EffectType;
        return SpellEffectType.None;
    }

    public SpellTargetFlags GetSpellTargetFlags(uint spellId)
    {
        return SpellTargetFlags.None;
    }

    public (SpellTarget, SpellTarget) GetSpellEffectTargetType(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return ((SpellTarget)effect.ImplicitTarget[0], (SpellTarget)effect.ImplicitTarget[1]);
        return (SpellTarget.NoTarget, SpellTarget.NoTarget);
    }

    public uint GetSpellEffectMiscValueA(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return effect.EffectMiscValueA;
        return 0;
    }

    public uint GetSpellEffectTriggerSpell(uint spellId, int index)
    {
        if (TryGetEffect(spellId, index, out var effect))
            return effect.EffectTriggerSpell;
        return 0;
    }

    public DBCVersions Version => DBCVersions.MOP_18414;
}