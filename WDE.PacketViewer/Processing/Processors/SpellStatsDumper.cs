using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ConsoleTables;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Services;
using WDE.PacketViewer.Utils;

namespace WDE.PacketViewer.Processing.Processors
{
    public class SpellStatsDumper : BaseSpellStatsDumper, IPacketTextDumper
    {
        public SpellStatsDumper(ISpellStore spellStore,
            ICachedDatabaseProvider databaseProvider,
            IDbcSpellService spellService) : base(spellStore, databaseProvider, spellService)
        {
        }
        
        public Task<string> Generate()
        {
            StringBuilder sb = new();
            Dictionary<uint, PerEntryStats> stats = GeneratePerEntryStatistics();

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
    }
}