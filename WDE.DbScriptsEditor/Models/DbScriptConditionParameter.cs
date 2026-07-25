using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.Models
{
    // A conditions.condition_entry root id carried in a step parameter (TERMINATE_COND's
    // datalong). The "..." picker opens the cmangos condition tree editor in memory; the edited
    // rows are stashed on the step (the dialog edits a detached copy) and only land in the
    // document's DbScriptConditionsStore when the edit dialog is accepted, so the SQL is bundled
    // with the script save exactly like the if-row conditions.
    public class DbScriptConditionParameter : Parameter, ICustomPickerContextualParameter<long>
    {
        private const uint TerminateCondCommand = 34;

        private readonly IMangosConditionService conditionService;

        public DbScriptConditionParameter(IMangosConditionService conditionService)
        {
            this.conditionService = conditionService;
        }

        public override string ToString(long value) => value == 0 ? "(no condition)" : $"condition {value}";

        public async Task<(long, bool)> PickValue(long value, object context)
        {
            if (context is not EditableDbScriptStep step)
                return (0, false);

            var root = (uint)value;
            // freshest first: a not-yet-accepted edit from this dialog session, then the
            // document's (possibly edited, unsaved) closure, then the database
            var known = step.PendingConditionEdits?.LastOrDefault(e => e.Lines.Any(l => l.ConditionEntry == root))?.Lines
                        ?? step.ConditionClosureProvider?.Invoke(root)
                        ?? await conditionService.LoadConditionsClosure(root);
            var localMax = known.Count == 0 ? 0 : known.Max(l => l.ConditionEntry);
            var firstFree = await conditionService.GetFirstFreeConditionEntry(localMax);

            var result = await conditionService.EditConditionTree(root, known, firstFree,
                step.CommandId == TerminateCondCommand ? "Terminate condition" : "Condition",
                SourceTargetOf(step));
            if (result == null)
                return (0, false);

            (step.PendingConditionEdits ??= new()).Add(new DbScriptPendingConditionEdit(
                firstFree,
                known.Select(l => l.ConditionEntry).ToList(),
                result.Lines));
            return (result.RootEntry, true);
        }

        private static MangosConditionSourceTarget SourceTargetOf(EditableDbScriptStep step)
        {
            // TERMINATE_COND picks the player explicitly: target = whichever of the resolved
            // source/target is a player, source = the other object (ScriptMgr.cpp command 34)
            if (step.CommandId == TerminateCondCommand)
                return new MangosConditionSourceTarget(
                    "the player (source or target, whichever is the player)",
                    "the other of source / target");
            var (source, target) = step.ResolveActors();
            return new MangosConditionSourceTarget(target, source);
        }
    }

    /// <summary>A condition edit made inside the edit-action dialog, applied to the document's
    /// conditions store only when the dialog is accepted.</summary>
    public record DbScriptPendingConditionEdit(
        uint FirstFree,
        IReadOnlyList<uint> PreviousEntries,
        IReadOnlyList<IMangosConditionLine> Lines);
}
