using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ConsoleTables;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Services;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class SpellStatsDumper : PacketProcessor<bool>, IPacketTextDumper
    {
        private readonly ISpellStore spellStore;
        private readonly IDatabaseProvider databaseProvider;
        private readonly ISpellService spellService;
        private readonly Dictionary<UniversalGuid, CreatureState> states = new();

        private class SpellHistory
        {
            public readonly List<DateTime> CastTimes = new();
            public DateTime CombatEnterTime = new();
            public bool SeenEnterCombat = false;
        }

        private class CreatureState
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

        public SpellStatsDumper(ISpellStore spellStore,
            IDatabaseProvider databaseProvider,
            ISpellService spellService)
        {
            this.spellStore = spellStore;
            this.databaseProvider = databaseProvider;
            this.spellService = spellService;
        }

        private CreatureState? GetState(UniversalGuid? guid)
        {
            if (guid == null || guid.Type != UniversalHighGuid.Creature &&
                guid.Type != UniversalHighGuid.Vehicle)
                return null;

            if (states.TryGetValue(guid, out var s))
                return s;

            return states[guid] = new();
        }

        protected override bool Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var obj in packet.Created)
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

            foreach (var obj in packet.Updated)
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

            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellStart packet)
        {
            var state = GetState(packet.Data.Caster);
            if (state == null)
                return false;

            if (!state.InCombat)
                return false;

            if (!state.SpellHistory.TryGetValue(packet.Data.Spell, out var history))
                history = state.SpellHistory[packet.Data.Spell] =
                    new() { new() { CombatEnterTime = state.LastCombatEnterTime, SeenEnterCombat = state.SeenEnterCombat} };

            history[^1].CastTimes.Add(basePacket.Time.ToDateTime());

            return base.Process(basePacket, packet);
        }

        public string CreatureName(uint entry)
        {
            var template = databaseProvider.GetCreatureTemplate(entry);
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

        private class Stats
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

        private class PerEntryStats
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

        public Task<string> Generate()
        {
            StringBuilder sb = new();
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

            var table = new ConsoleTable("Creature", "Spell", "Cast time", "Usage", "Average (ms)", "Min (ms)",
                "Max (ms)", "Count", "@", "Average (ms)", "Min (ms)", "Max (ms)", "Count", "@", "Average (ms)",
                "Min (ms)", "Max (ms)", "Count");
            table.SetCellSpan(0, 0, 4);
            table.SetCellSpan(0, 4, 4);
            table.SetCellSpan(0, 9, 4);
            table.SetCellSpan(0, 14, 4);
            table.SetCellAlignment(0, 4, Alignment.Center);
            table.SetCellAlignment(0, 9, Alignment.Center);
            table.SetCellAlignment(0, 14, Alignment.Center);
            table.AddRow("", "", "", "", "INITIAL", null, null, null, "", "REPEAT", null, null, null, "",
                "COMBAT LENGTH", null, null, null);
            foreach (var pair in stats)
            {
                int i = 0;
                foreach (var spell in pair.Value.Spells)
                {
                    if (spell.Value.initial.DiffsCount == 0)
                        continue;

                    var castTime = spellService.GetSpellCastingTime(spell.Key);

                    bool hasRepeat = spell.Value.repeat.DiffsCount > 0;

                    var name = i == 0 ? CreatureName(pair.Key) : "";
                    if (i == 0)
                        table.AddDoubleRowDivider();
                    table.AddRow(name,
                        SpellName(spell.Key),
                        castTime == null || castTime.Value == TimeSpan.Zero ? "" : (castTime.Value.TotalSeconds + " s"),
                        pair.Value.CombatTimes.DiffsCount == 0
                            ? ""
                            : (int)(100.0f * spell.Value.initial.DiffsCount / pair.Value.CombatTimes.DiffsCount) + "%",
                        spell.Value.initial.AvgDiff,
                        spell.Value.initial.DiffsCount == 1 ? "" : spell.Value.initial.MinDiff,
                        spell.Value.initial.DiffsCount == 1 ? "" : spell.Value.initial.MaxDiff,
                        spell.Value.initial.DiffsCount,
                        null,
                        hasRepeat ? spell.Value.repeat.AvgDiff : "",
                        hasRepeat && spell.Value.repeat.DiffsCount > 1 ? spell.Value.repeat.MinDiff : "",
                        hasRepeat && spell.Value.repeat.DiffsCount > 1 ? spell.Value.repeat.MaxDiff : "",
                        hasRepeat ? spell.Value.repeat.DiffsCount : "",
                        null,
                        i == 0 && pair.Value.CombatTimes.DiffsCount > 0 ? pair.Value.CombatTimes.AvgDiff : "",
                        i == 0 && pair.Value.CombatTimes.DiffsCount > 0
                            ? pair.Value.CombatTimes.DiffsCount == 1 ? "" : pair.Value.CombatTimes.MinDiff
                            : "",
                        i == 0 && pair.Value.CombatTimes.DiffsCount > 0
                            ? pair.Value.CombatTimes.DiffsCount == 1 ? "" : pair.Value.CombatTimes.MaxDiff
                            : "",
                        i == 0 ? pair.Value.CombatTimes.DiffsCount : "");

                    if (i < pair.Value.Spells.Count - 1)
                    {
                        table.DisableNextRowCellTopBorder(0);
                        table.DisableNextRowCellTopBorder(8);
                        table.DisableNextRowCellTopBorder(13);
                        table.DisableNextRowCellTopBorder(14);
                        table.DisableNextRowCellTopBorder(15);
                        table.DisableNextRowCellTopBorder(16);
                        table.DisableNextRowCellTopBorder(17);
                    }
                    
                    i++;
                }
            }

            sb.AppendLine(table.ToStringAlternative());

            sb.AppendLine("\n\n");
            sb.AppendLine(" -- HELP --");
            sb.AppendLine("Cast time - cast time of the spell extracted from DBC, can be useful I guess");
            sb.AppendLine("Usage (%) - in how many battles the given spell has been used");
            sb.AppendLine("Initial - after how many seconds after the start of a combat, the spell was firstly used");
            sb.AppendLine("Repeat - after how many seconds after the previous cast of this spell, it was used again");
            sb.AppendLine("Combat length - how long the combat lasted");
            sb.AppendLine("\n\n");
            sb.AppendLine("NOTE: the beginning of the combat is the moment when IN_COMBAT flag has been sent to the client. If the servers sends\nan already spawned creature that is already in combat, it will not be counted in initial timings\n(because we don't know when the combat started)");
            sb.AppendLine("\nNOTE 2: the initial combats can be wrong when you kill NPCs too fast. That's why you should take a look at combat length as well.");
            
#if DEBUG
            sb.AppendLine("\n\n === BREAKDOWN ===");
            foreach (var pair in states)
            {
                bool intro = false;
                foreach (var spell in pair.Value.SpellHistory)
                {
                    bool spellIntro = false;
                    foreach (var series in spell.Value)
                    {
                        if (series.CastTimes.Count == 0)
                            continue;
                        DateTime last = series.CombatEnterTime;
                        foreach (var time in series.CastTimes)
                        {
                            if (!intro)
                            {
                                intro = true;
                                sb.AppendLine(pair.Key.ToWowParserString());
                            }

                            if (!spellIntro)
                            {
                                spellIntro = true;
                                sb.AppendLine("  > " + SpellName(spell.Key) + ":");
                            }

                            sb.AppendLine("      - " + time + " (+" + (time - last).TotalMilliseconds + " ms)");
                            last = time;
                        }

                        sb.AppendLine("");
                    }

                    sb.AppendLine("");
                }
            }
#endif

            return Task.FromResult(sb.ToString());
        }

        public int GetMedian(int[] array)
        {
            return 2;
        }
    }
}