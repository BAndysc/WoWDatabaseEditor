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
        public const uint EmoteCommandId = 1;
        public const uint QuestExploredCommandId = 7;
        public const uint RespawnGameObjectCommandId = 9;
        public const uint OpenDoorCommandId = 11;
        public const uint CloseDoorCommandId = 12;
        public const uint CastSpellCommandId = 15;
        public const uint PlaySoundCommandId = 16;
        public const uint CreateItemCommandId = 17;
        public const uint GoLockStateCommandId = 27;
        public const uint TerminateScriptCommandId = 31;
        public const uint TerminateCondCommandId = 34;
        public const uint SetFacingCommandId = 36;
        public const uint MoveDynamicCommandId = 37;
        public const uint StartRelayScriptCommandId = 45;
        public const uint CastCustomSpellCommandId = 46;
        public const uint SpawnGroupCommandId = 51;
        public const uint RecallAccessoriesCommandId = 56;

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
                var decoded = DbScriptFlagsCodec.Decode(line.DataFlags, line.BuddyEntry, line.SearchRadius, step.BuddyCapability);

                // a) nonzero value in a column the command/variant doesn't use
                foreach (var col in step.UnusedColumns)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        $"Column '{col.ToLowerInvariant()}' has a nonzero value but this command doesn't use it."));

                // b) command-additional flag on a command that doesn't support it. SET_FACING is
                // special: the server whitelists 0x8 for it at load but never reads it.
                if (decoded.CommandAdditional && command != null && !command.SupportsAdditionalFlag)
                {
                    if (command.Id == SetFacingCommandId)
                        diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                            "data_flags 0x8 is accepted by the server for SET_FACING but has no effect."));
                    else
                        diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                            "data_flags 0x8 (command-additional) is set but this command has no additional behaviour."));
                }

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

                // f) player-targeting command in a script type that may have no player.
                // TERMINATE_COND is exempt: its condition evaluates fine without a player
                // (only the fail-quest part needs one).
                if (command != null && command.Id != TerminateCondCommandId &&
                    IsPlayerOnlyTarget(command.TargetTypes) && PlayerlessScriptTypes.Contains(typeInfo.Type))
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        $"This command acts on a player, but '{typeInfo.ReadableName}' scripts may run without one."));

                // h) rows the server refuses to load (LoadScripts `continue`s with an error)
                if (line.Command == TalkCommandId && line.DataLong == 0 && line.DataInt == 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "TALK has neither a text nor a random template; the server skips this row at load."));
                if ((line.Command == RespawnGameObjectCommandId || line.Command == OpenDoorCommandId ||
                     line.Command == CloseDoorCommandId) && line.DataLong == 0 && !decoded.Buddy.Provided)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "This command needs a gameobject guid or a buddy; the server skips this row at load."));
                if (line.Command == CastCustomSpellCommandId &&
                    line.DataInt == 0 && line.DataInt2 == 0 && line.DataInt3 == 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "CAST_CUSTOM_SPELL needs at least one nonzero base points value; the server skips this row at load."));
                if ((line.DataFlags & ~DbScriptFlags.ModeledMask) != 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        $"data_flags has invalid bits (0x{line.DataFlags & ~DbScriptFlags.ModeledMask:X}); the server skips this row at load."));
                if (line.Command == QuestExploredCommandId && line.DataLong2 != 0 &&
                    (line.DataLong2 < 5 || line.DataLong2 > 90))
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "QUEST_EXPLORED distance must be 0 (disabled) or between 5 and 90; the server skips this row at load."));
                if (line.Command == CreateItemCommandId && line.DataLong2 == 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "CREATE_ITEM amount is 0; the server skips this row at load."));
                if (line.Command == GoLockStateCommandId &&
                    (line.DataLong == 0 || line.DataLong >= 0x10 ||
                     ((line.DataLong & 0x1) != 0 && (line.DataLong & 0x2) != 0) ||
                     ((line.DataLong & 0x4) != 0 && (line.DataLong & 0x8) != 0)))
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "GO_LOCK_STATE lock state is invalid (0, above 0xF, lock+unlock or no-interact+interact); the server skips this row at load."));
                if (line.Command == MoveDynamicCommandId && line.DataLong < line.DataLong2)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "MOVE_DYNAMIC max distance is below min distance; the server skips this row at load."));
                if (line.Command == RecallAccessoriesCommandId && (line.DataInt < -1 || line.DataInt >= 8))
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "Seat index must be -1 (all seats) or 0-7; the server skips this row at load."));
                if (line.Command == SpawnGroupCommandId && line.DataLong == 151 && line.DataLong2 == 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Error,
                        "Remove-formation with spawn group 0 is rejected at server load (the own-group form only works for create)."));

                // music (0x8) delivery honours only the target-player and zone/area-wide flags
                if (line.Command == PlaySoundCommandId && decoded.CommandAdditional && (line.DataLong2 & 0x6) != 0)
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                        "The distance-dependent and map-wide sound flags are ignored when playing music."));

                // i) random text/emote/spell slots are zero-terminated on the server: it stops
                // reading at the first empty slot, so anything after a gap never plays
                if (line.Command == TalkCommandId || line.Command == EmoteCommandId ||
                    line.Command == CastSpellCommandId)
                {
                    var slots = new[] { line.DataInt, line.DataInt2, line.DataInt3, line.DataInt4 };
                    var gap = false;
                    foreach (var slot in slots)
                    {
                        if (slot == 0)
                            gap = true;
                        else if (gap)
                        {
                            diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning,
                                "Random slots after an empty one are dead: the server stops reading at the first zero."));
                            break;
                        }
                    }
                }

                // g) structural source/target/buddy advisories (inert bits, ineffective flags,
                // kind mismatch, dangling condition buddy on the wrong command...)
                foreach (var warning in DbScriptStructuralValidator.Validate(decoded, command))
                    diags.Add(new DbScriptDiagnostic(DbScriptDiagnosticSeverity.Warning, warning));
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
            // The core's termination condition channels: a search entry (datalong), a pool id
            // (datalong3) or a by-guid buddy each make the terminate conditional on the search
            // result (inverted by data_flags 0x8).
            if (line.DataLong != 0 || line.DataLong3 != 0)
                return false;
            var decoded = DbScriptFlagsCodec.Decode(line.DataFlags, line.BuddyEntry, line.SearchRadius, step.BuddyCapability);
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
