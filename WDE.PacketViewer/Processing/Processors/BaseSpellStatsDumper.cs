using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Services;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors;

public abstract unsafe class BaseSpellStatsDumper : PacketProcessor<bool>
{
    protected readonly ISpellStore spellStore;
    protected readonly ICachedDatabaseProvider databaseProvider;
    protected readonly IDbcSpellService spellService;
    protected readonly Dictionary<UniversalGuid, CreatureState> states = new();

    public class SpellHistory
    {
        public readonly List<DateTime> CastTimes = new();
        public DateTime CombatEnterTime = new();
        public bool SeenEnterCombat = false;
    }

    public class CreatureState
    {
        public bool InCombat;
        public DateTime LastCombatEnterTime;
        public bool SeenEnterCombat;
        public readonly List<TimeSpan> CombatTimes = new();
        public readonly Dictionary<uint, List<SpellHistory>> SpellHistory = new();

        public void ResetState()
        {
            foreach (var pair in SpellHistory)
            {
                if (pair.Value.Count > 0 && pair.Value[^1].CastTimes.Count > 0)
                    pair.Value.Add(new() { CombatEnterTime = LastCombatEnterTime, SeenEnterCombat = SeenEnterCombat});
                else if (pair.Value.Count > 0)
                {
                    pair.Value[^1].CombatEnterTime = LastCombatEnterTime;
                    pair.Value[^1].SeenEnterCombat = SeenEnterCombat;
                }
            }
        }
    }

    public BaseSpellStatsDumper(ISpellStore spellStore,
        ICachedDatabaseProvider databaseProvider,
        IDbcSpellService spellService)
    {
        this.spellStore = spellStore;
        this.databaseProvider = databaseProvider;
        this.spellService = spellService;
    }

    private CreatureState? GetState(UniversalGuid? guid)
    {
        if (guid == null || guid.Value.Type != UniversalHighGuid.Creature &&
            guid.Value.Type != UniversalHighGuid.Vehicle)
            return null;

        if (states.TryGetValue(guid.Value, out var s))
            return s;

        return states[guid.Value] = new();
    }

    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
    {
        foreach (ref readonly var obj in packet.Created.AsSpan())
        {
            var state = GetState(obj.Guid);
            if (state == null)
                continue;

            if (obj.Values.TryGetInt("UNIT_FIELD_FLAGS", out var flags))
            {
                state.InCombat = (flags & (uint)GameDefines.UnitFlags.InCombat) == (uint)GameDefines.UnitFlags.InCombat;
                state.LastCombatEnterTime = basePacket.Time.ToDateTime();
                state.SeenEnterCombat = false;
                if (state.InCombat)
                    state?.ResetState();
            }
            else
                state.InCombat = false;
        }

        foreach (ref readonly var obj in packet.Updated.AsSpan())
        {
            var state = GetState(obj.Guid);
            if (state == null)
                continue;

            if (obj.Values.TryGetInt("UNIT_FIELD_FLAGS", out var flags))
            {
                var nowInCombat = (flags & (uint)GameDefines.UnitFlags.InCombat) == (uint)GameDefines.UnitFlags.InCombat;
                if (state.InCombat != nowInCombat)
                {
                    if (!nowInCombat)
                    {
                        state.CombatTimes.Add(basePacket.Time.ToDateTime() - state.LastCombatEnterTime);
                    }
                    else
                    {
                        state.SeenEnterCombat = true;
                        state.LastCombatEnterTime = basePacket.Time.ToDateTime();
                        state.ResetState();
                    }

                    state.InCombat = nowInCombat;
                }
            }
        }

        return base.Process(in basePacket, in packet);
    }

    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellStart packet)
    {
        if (packet.Data == null)
            return false;
        var state = GetState(packet.Data->Caster);
        if (state == null)
            return false;

        if (!state.InCombat)
            return false;

        if (!state.SpellHistory.TryGetValue(packet.Data->Spell, out var history))
            history = state.SpellHistory[packet.Data->Spell] =
                new() { new() { CombatEnterTime = state.LastCombatEnterTime, SeenEnterCombat = state.SeenEnterCombat} };

        history[^1].CastTimes.Add(basePacket.Time.ToDateTime());

        return base.Process(in basePacket, in packet);
    }

    public string CreatureName(uint entry)
    {
        var template = databaseProvider.GetCachedCreatureTemplate(entry);
        if (template != null)
            return $"{entry,7} {template.Name}";
        return $"{entry,7}";
    }

    public string SpellName(uint entry)
    {
        if (spellStore.HasSpell(entry))
            return $"{entry,7} {spellStore.GetName(entry)!}";
        return $"{entry,7}";
    }

    public class Stats
    {
        private TimeSpan diffsSum = TimeSpan.Zero;
        public int DiffsCount = 0;
        private TimeSpan diffMin = TimeSpan.MaxValue;
        private TimeSpan diffMax = TimeSpan.MinValue;

        public void Report(TimeSpan diff)
        {
            diffsSum += diff;
            DiffsCount += 1;
            diffMin = diff < diffMin ? diff : diffMin;
            diffMax = diff > diffMax ? diff : diffMax;
        }


        public int MinDiff => RoundNearest50((int)diffMin.TotalMilliseconds);
        public int MaxDiff => RoundNearest50((int)diffMax.TotalMilliseconds);
        public int AvgDiff => RoundNearest50((int)(diffsSum.TotalMilliseconds / DiffsCount));
    }

    public static int RoundNearest50(int value)
    {
        return (value + 25) / 50 * 50;
    }

    public class PerEntryStats
    {
        public readonly Stats CombatTimes = new();
        public readonly Dictionary<uint, (Stats initial, Stats repeat)> Spells = new();

        public (Stats initial, Stats repeat) GetSpellStats(uint entry)
        {
            if (Spells.TryGetValue(entry, out var pair))
                return pair;
            return Spells[entry] = (new(), new());
        }
    }

    protected Dictionary<uint, PerEntryStats> GeneratePerEntryStatistics()
    {
        Dictionary<uint, PerEntryStats> stats = new();

        foreach (var unit in states)
        {
            if (!stats.TryGetValue(unit.Key.Entry, out var unitStats))
                unitStats = stats[unit.Key.Entry] = new();

            foreach (var combatTime in unit.Value.CombatTimes)
                unitStats.CombatTimes.Report(combatTime);

            foreach (var spell in unit.Value.SpellHistory)
            {
                var spellStats = unitStats.GetSpellStats(spell.Key);

                foreach (var times in spell.Value)
                {
                    var prevTime = times.CombatEnterTime;
                    for (var index = 0; index < times.CastTimes.Count; index++)
                    {
                        var thisTime = times.CastTimes[index];
                        var diff = thisTime - prevTime;
                        if (index == 0 && times.SeenEnterCombat)
                        {
                            spellStats.initial.Report(diff);
                        }
                        else
                        {
                            if (diff > TimeSpan.Zero)
                                spellStats.repeat.Report(diff);
                        }

                        prevTime = thisTime;
                    }
                }
            }
        }
        return stats;
    }
}