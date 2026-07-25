using System.Collections.Generic;
using System.Linq;

namespace WDE.DbScriptsEditor.Models
{
    public enum DbScriptDiagnosticSeverity
    {
        Info,
        Warning,
        Error,
    }

    public class DbScriptDiagnostic
    {
        public DbScriptDiagnosticSeverity Severity { get; }
        public string Message { get; }

        public DbScriptDiagnostic(DbScriptDiagnosticSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    // Synchronous, DB-free inspections over a whole script. Returns one diagnostics list per step
    // (aligned to the input order). Async/DB checks (relay id existence, broadcast_text presence)
    // are intentionally out of scope here.
    public static class DbScriptInspections
    {
        public const uint TalkCommandId = 0;
        public const uint TerminateScriptCommandId = 31;
        public const uint TerminateCondCommandId = 34;
        public const uint StartRelayScriptCommandId = 45;

        // Commands that act on a player (target is exactly a Player) and so misbehave in script
        // types that have no guaranteed player at runtime.
        private static readonly HashSet<DbScriptType> PlayerlessScriptTypes = new()
        {
            DbScriptType.CreatureMovement, DbScriptType.GoUse, DbScriptType.GoTemplateUse,
            DbScriptType.Event, DbScriptType.Relay,
        };

        public static IReadOnlyList<IReadOnlyList<DbScriptDiagnostic>> Inspect(
            IReadOnlyList<EditableDbScriptStep> steps, DbScriptTypeInfo typeInfo)
        {
            var result = new List<List<DbScriptDiagnostic>>(steps.Count);
            for (var i = 0; i < steps.Count; i++)
                result.Add(new List<DbScriptDiagnostic>());

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var diags = result[i];
                var command = step.Command;
                var line = step.ToLine();
                var decoded = DbScriptFlagsCodec.Decode(line.DataFlags, line.BuddyEntry, line.SearchRadius);

                // a) nonzero value in a column the command/variant doesn't use
                foreach (var col in step.UnusedColumns)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        $"Column '{col.ToLowerInvariant()}' has a nonzero value but this command doesn't use it."));

                // b) command-additional flag on a command that doesn't support it
                if (decoded.CommandAdditional && command != null && !command.SupportsAdditionalFlag)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "data_flags 0x8 (command-additional) is set but this command has no additional behaviour."));

                // c) a random template set alongside the fixed value it replaces — the core always
                // prefers the template, so the fixed value is dead data
                if (line.Command == TalkCommandId && line.DataLong != 0 &&
                    (line.DataInt != 0 || line.DataInt2 != 0 || line.DataInt3 != 0 || line.DataInt4 != 0))
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        "TALK has both fixed texts and a random template; the core ignores the texts and always picks from the template."));
                if (line.Command == StartRelayScriptCommandId && line.DataLong != 0 && line.DataLong2 != 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        "START_RELAY_SCRIPT has both a fixed relay id and a random template; the core ignores the relay id and always picks from the template."));

                // d) TERMINATE_COND without a condition
                if (line.Command == TerminateCondCommandId && line.ConditionId == 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        "TERMINATE_COND has no condition_id, so it terminates unconditionally."));

                // f) player-targeting command in a script type that may have no player
                if (command != null && IsPlayerOnlyTarget(command.TargetTypes) && PlayerlessScriptTypes.Contains(typeInfo.Type))
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        $"This command acts on a player, but '{typeInfo.ReadableName}' scripts may run without one."));
            }

            // e) steps that can never run because an earlier unconditional TERMINATE_SCRIPT cancels them
            FlagUnreachableAfterTerminate(steps, result);

            return result;
        }

        private static void FlagUnreachableAfterTerminate(
            IReadOnlyList<EditableDbScriptStep> steps, List<List<DbScriptDiagnostic>> result)
        {
            // execution order = ascending (delay, priority); original index breaks ties
            var order = Enumerable.Range(0, steps.Count)
                .OrderBy(i => (uint)steps[i].Delay.Value)
                .ThenBy(i => steps[i].Priority)
                .ThenBy(i => i)
                .ToList();

            var terminatedAt = -1;
            for (var pos = 0; pos < order.Count; pos++)
            {
                var step = steps[order[pos]];
                if (terminatedAt >= 0 && (uint)step.Delay.Value > (uint)steps[terminatedAt].Delay.Value)
                {
                    result[order[pos]].Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        "This step is scheduled after an unconditional TERMINATE_SCRIPT and may never run."));
                    continue;
                }

                if (IsUnconditionalTerminate(step))
                {
                    if (terminatedAt < 0)
                        terminatedAt = order[pos];
                }
            }
        }

        private static bool IsUnconditionalTerminate(EditableDbScriptStep step)
        {
            if (step.CommandId != TerminateScriptCommandId)
                return false;
            var line = step.ToLine();
            if (line.ConditionId != 0)
                return false;
            var decoded = DbScriptFlagsCodec.Decode(line.DataFlags, line.BuddyEntry, line.SearchRadius);
            return !decoded.Buddy.Provided; // a buddy search makes the terminate conditional
        }

        private static bool IsPlayerOnlyTarget(IReadOnlyList<string> targetTypes)
        {
            if (targetTypes.Count == 0)
                return false;
            var sawPlayer = false;
            foreach (var t in targetTypes)
            {
                if (string.Equals(t, "Player", System.StringComparison.OrdinalIgnoreCase))
                    sawPlayer = true;
                else if (!string.Equals(t, "None", System.StringComparison.OrdinalIgnoreCase))
                    return false; // some other object type is acceptable → not player-only
            }
            return sawPlayer;
        }
    }
}
