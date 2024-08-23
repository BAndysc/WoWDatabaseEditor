using System;
using System.Collections.Generic;
using System.IO;
using WDE.Common.Services;

namespace WDE.DbcStore.Spells.Retail;

public class RetailSpellService : IDbcSpellService, IDbcSpellLoader
{
    private readonly DBCD.DBCD dbcd;

    private readonly Dictionary<uint, SpellData> spells = new();
    private readonly List<SpellData> spellsList = new();

    private struct SpellCastingRequirements
    {
        public byte FacingCasterFlags;
        public ushort MinFactionID;
        public int MinReputation;
        public ushort RequiredAreasID;
        public byte RequiredAuraVision;
        public ushort RequiresSpellFocus;
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

    private struct SpellCastTime
    {
        public uint BaseTimeMs;
        public uint MinimumMs;
    }

    private struct SpellDuration
    {
        public uint Duration;
        public uint MaxDuration;
    }

    private struct SpellCooldown
    {
        public uint CategoryRecoveryTime;
        public uint RecoveryTime;
        public uint StartRecoveryTime;
        public uint DifficultyID;
    }

    private class SpellData
    {
        public uint Id;
        public uint[] Attributes = new uint[15];
        public string Name = "";
        public SpellCastTime? CastTime;
        public SpellDuration? Duration;
        public SpellCooldown? Cooldown;
        public SpellTargetFlags SpellTargetFlags;
        public SpellEffect[]? Effects;
        public SpellCastingRequirements[]? Requirements;
    }

    public RetailSpellService(DBCD.DBCD dbcd)
    {
        this.dbcd = dbcd;
    }

    public bool Exists(uint spellId) => spells.ContainsKey(spellId);

    public int SpellCount => spellsList.Count;

    public uint GetSpellId(int index) => spellsList[index].Id;

    public string GetName(uint spellId)
    {
        return TryGetSpellData(spellId)?.Name ?? "";
    }

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
        if (spells.TryGetValue(spellId, out var spell))
            if (spell.Requirements != null && spell.Requirements.Length > 0)
                return spell.Requirements[0].RequiresSpellFocus;
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

    public DBCVersions Version => DBCVersions.RETAIL;

    private SpellData GetOrCreateSpellData(uint spellId)
    {
        if (spells.TryGetValue(spellId, out var spell))
            return spell;

        spell = spells[spellId] = new SpellData()
        {
            Id = spellId
        };
        spellsList.Add(spell);
        return spell;
    }

    private SpellData? TryGetSpellData(uint spellId)
    {
        return spells.GetValueOrDefault(spellId);
    }

    public void Load(string path, DBCLocales dbcLocale)
    {
        using var spellNamesStream = File.OpenRead($"{path}/SpellName.db2");
        using var spellEffectsStream = File.OpenRead($"{path}/SpellEffect.db2");
        using var spellDurationStream = File.OpenRead($"{path}/SpellDuration.db2");
        using var spellCastTimesStream = File.OpenRead($"{path}/SpellCastTimes.db2");
        using var spellMiscStream = File.OpenRead($"{path}/SpellMisc.db2");
        using var spellCooldownsStream = File.OpenRead($"{path}/SpellCooldowns.db2");
        using var spellTargetRestrictionsStream = File.OpenRead($"{path}/SpellTargetRestrictions.db2");
        using var spellCastingRequirementsStream = File.OpenRead($"{path}/SpellCastingRequirements.db2");

        var namesStorage = dbcd.Load(spellNamesStream, "SpellName");
        var effectsStorage = dbcd.Load(spellEffectsStream, "SpellEffect");
        var spellDurationStorage = dbcd.Load(spellDurationStream, "SpellDuration");
        var spellCastTimesStorage = dbcd.Load(spellCastTimesStream, "SpellCastTimes");
        var spellMiscStorage = dbcd.Load(spellMiscStream, "SpellMisc");
        var spellCooldownsStorage = dbcd.Load(spellCooldownsStream, "SpellCooldowns");
        var spellTargetRestrictionsStorage = dbcd.Load(spellTargetRestrictionsStream, "SpellTargetRestrictions");
        var spellCastingRequirementsStorage = dbcd.Load(spellCastingRequirementsStream, "SpellCastingRequirements");

        Dictionary<uint, SpellCastTime> spellCastTimes = new();
        Dictionary<uint, SpellDuration> spellDuration = new();

        foreach (var row in namesStorage.Values)
        {
            var id = (uint)row.ID;
            GetOrCreateSpellData(id).Name = row.FieldAs<string>("Name_lang");
        }

        foreach (var row in spellCastTimesStorage.Values)
        {
            var id = (uint)row.ID;
            var baseTimeMs = (uint)row.FieldAs<int>("Base");
            var minimumMs = (uint)row.FieldAs<int>("Minimum");

            spellCastTimes[id] = new SpellCastTime()
            {
                BaseTimeMs = baseTimeMs,
                MinimumMs = minimumMs
            };
        }

        foreach (var row in spellDurationStorage.Values)
        {
            var id = (uint)row.ID;
            var duration = (uint)row.FieldAs<int>("Duration");
            var maxDuration = (uint)row.FieldAs<int>("MaxDuration");

            spellDuration[id] = new SpellDuration()
            {
                Duration = duration,
                MaxDuration = maxDuration
            };
        }

        foreach (var row in spellMiscStorage.Values)
        {
            var id = (uint)row.ID;
            var castingTimeIndex = row.FieldAs<ushort>("CastingTimeIndex");
            var durationIndex = row.FieldAs<ushort>("DurationIndex");
            var rangeIndex = row.FieldAs<ushort>("RangeIndex");
            var spellId = (uint)row.FieldAs<int>("SpellID");
            var attributes = row.FieldAs<int[]>("Attributes");

            var spell = GetOrCreateSpellData(spellId);

            for (int i = 0; i < 15; ++i)
                spell.Attributes[i] = (uint)attributes[i];

            if (spellCastTimes.TryGetValue(castingTimeIndex, out var castTime))
                spell.CastTime = castTime;

            if (spellDuration.TryGetValue(durationIndex, out var duration))
                spell.Duration = duration;
        }

        foreach (var row in spellCooldownsStorage.Values)
        {
            var spell = GetOrCreateSpellData((uint)row.FieldAs<int>("SpellID"));

            spell.Cooldown = new SpellCooldown()
            {
                CategoryRecoveryTime = (uint)row.FieldAs<int>("CategoryRecoveryTime"),
                RecoveryTime = (uint)row.FieldAs<int>("RecoveryTime"),
                StartRecoveryTime = (uint)row.FieldAs<int>("StartRecoveryTime"),
                DifficultyID = row.FieldAs<byte>("DifficultyID")
            };
        }

        foreach (var row in spellTargetRestrictionsStorage.Values)
        {
            var targets = row.FieldAs<int>("Targets");
            var spellID = (uint)row.FieldAs<int>("SpellID");

            if (spells.TryGetValue(spellID, out var spell))
                spell.SpellTargetFlags = (SpellTargetFlags)targets;
        }

        foreach (var row in spellCastingRequirementsStorage.Values)
        {
            var spell = GetOrCreateSpellData((uint)row.FieldAs<int>("SpellID"));
            Array.Resize(ref spell.Requirements, (spell.Requirements?.Length ?? 0) + 1);
            spell.Requirements[^1] = new SpellCastingRequirements()
            {
                FacingCasterFlags = row.FieldAs<byte>("FacingCasterFlags"),
                MinFactionID = row.FieldAs<ushort>("MinFactionID"),
                MinReputation = row.FieldAs<int>("MinReputation"),
                RequiredAreasID = row.FieldAs<ushort>("RequiredAreasID"),
                RequiredAuraVision = row.FieldAs<byte>("RequiredAuraVision"),
                RequiresSpellFocus = row.FieldAs<ushort>("RequiresSpellFocus"),
            };
        }

        foreach (var row in effectsStorage.Values)
        {
            var diffId = row.FieldAs<int>("DifficultyID");
            var effectIndex = row.FieldAs<int>("EffectIndex");
            int[] miscValue = row.FieldAs<int[]>("EffectMiscValue");
            short[] implicitTargets = row.FieldAs<short[]>("ImplicitTarget");

            var spell = GetOrCreateSpellData((uint)row.FieldAs<int>("SpellID"));

            if (spell.Effects == null || spell.Effects.Length <= effectIndex)
                Array.Resize(ref spell.Effects, Math.Max(3, effectIndex + 1));

            spell.Effects[effectIndex] = new SpellEffect()
            {
                Type = (SpellEffectType)row.FieldAs<uint>("Effect"),
                AuraType = (SpellAuraType)row.FieldAs<short>("EffectAura"),
                TargetA = (SpellTarget)implicitTargets[0],
                TargetB = (SpellTarget)implicitTargets[1],
                MiscValueA = miscValue[0],
                MiscValueB = miscValue[1],
                TriggerSpell = (uint)row.FieldAs<int>("EffectTriggerSpell")
            };
        }
    }
}