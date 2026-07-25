using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.DbScriptsEditor.Data;
using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.Models
{
    // The editable in-memory representation of one dbscript. The document is a flat list of ROWS
    // (action | wait | comment) — all equal, independently orderable editor entities. Delays and
    // stored comments are DERIVED on save: waits accumulate into the delay column, comment rows
    // attach to the next action's comment column (trailing comments become a fake command-0 line).
    public class EditableDbScript : BulkEditableBase
    {
        private readonly IDbScriptDataManager dataManager;
        private readonly IParameterFactory factory;

        public DbScriptType Type { get; }
        public DbScriptTypeInfo TypeInfo { get; }
        public uint ScriptId { get; }

        public ObservableCollection<DbScriptRow> Rows { get; } = new();

        // Only the action rows, in row order (snapshot). Delay values are kept in sync with the
        // wait rows by SyncDerived, so inspections/export can rely on them.
        public List<EditableDbScriptStep> Steps => Rows.OfType<EditableDbScriptStep>().ToList();

        public EditableDbScript(DbScriptType type, uint scriptId, IDbScriptDataManager dataManager, IParameterFactory factory)
        {
            Type = type;
            ScriptId = scriptId;
            TypeInfo = DbScriptTypes.GetInfo(type);
            this.dataManager = dataManager;
            this.factory = factory;
            Rows.CollectionChanged += OnRowsChanged;
        }

        public EditableDbScriptStep MakeStep(IDbScriptLine row) =>
            new(row, dataManager, TypeInfo, factory);

        public DbScriptWaitRow MakeWait(long durationMs) => new(factory, durationMs);

        public DbScriptCommentRow MakeComment(string text) => new(factory, text);

        public DbScriptIfRow MakeIf(long conditionId) => new(factory, conditionId);

        // DB lines → editor rows: sorted by (delay, priority); a delay increase becomes a wait
        // row; the custom half of a stored comment becomes comment row(s) above the action; fake
        // command-0 comment-only lines become pure comment rows.
        public void Load(IReadOnlyList<IDbScriptLine> lines)
        {
            // Suppress the per-insert SyncDerived: it would zero each step's condition id before
            // DeriveIfRows had a chance to read it (no if rows exist yet during loading).
            loading = true;
            try
            {
                long t = 0;
                foreach (var line in lines.OrderBy(l => l.Delay).ThenBy(l => l.Priority))
                {
                    if (line.Delay > t)
                    {
                        Rows.Add(MakeWait(line.Delay - t));
                        t = line.Delay;
                    }

                    var custom = DbScriptCommentConvention.ExtractCustom(line.Comments);
                    if (custom.Length > 0)
                        foreach (var part in DbScriptCommentConvention.Split(custom))
                            Rows.Add(MakeComment(part));

                    if (!DbScriptCommentConvention.IsFakeCommentLine(line))
                        Rows.Add(MakeStep(line));
                }
                DeriveIfRows();
            }
            finally
            {
                loading = false;
            }
            SyncDerived();
        }

        // Consecutive actions sharing the same nonzero condition_id come back as one "if" block:
        // an if row above the run, members flagged InIf (including waits/comments between them).
        private void DeriveIfRows()
        {
            var blocks = DbScriptConditionBlocks.Compute(
                Rows.Select(r => (r is EditableDbScriptStep,
                    r is EditableDbScriptStep s ? s.ConditionId.Value : 0)).ToList());
            for (var b = blocks.Count - 1; b >= 0; b--)
            {
                for (var i = blocks[b].Start; i <= blocks[b].End; i++)
                    Rows[i].InIf = true;
                Rows.Insert(blocks[b].Start, MakeIf(blocks[b].ConditionId));
            }
        }

        // Recomputes every derived value from the row order: wait start times, each action's
        // delay column (t = sum of preceding wait durations) and each action's condition_id
        // column (the ConditionId of the enclosing if block, 0 outside any block). Cheap, run
        // after any row change. Raises DerivedChanged so the VM can refresh indentation.
        public void SyncDerived()
        {
            long t = 0;
            long condition = 0;
            var blockOpen = false;
            foreach (var row in Rows)
            {
                if (row is DbScriptIfRow ifRow)
                {
                    condition = ifRow.ConditionId.Value;
                    blockOpen = true;
                    continue;
                }
                if (blockOpen && !row.InIf)
                    blockOpen = false;
                switch (row)
                {
                    case DbScriptWaitRow wait:
                        wait.StartsAt = t;
                        t += wait.Duration.Value;
                        break;
                    case EditableDbScriptStep step:
                        step.Delay.Value = t;
                        step.ConditionId.Value = blockOpen ? condition : 0;
                        break;
                }
            }
            DerivedChanged?.Invoke();
        }

        public event System.Action? DerivedChanged;

        // True membership per row (parallel to Rows): a row is a member of an if block only when
        // it sits in the unbroken InIf run directly after an if row. A stale InIf flag with no if
        // row above it counts as NOT a member.
        public List<bool> ComputeMembership()
        {
            var result = new List<bool>(Rows.Count);
            var blockOpen = false;
            foreach (var row in Rows)
            {
                if (row is DbScriptIfRow)
                {
                    blockOpen = true;
                    result.Add(false); // the if row itself is never indented/nested
                    continue;
                }
                if (blockOpen && !row.InIf)
                    blockOpen = false;
                result.Add(blockOpen);
            }
            return result;
        }

        // The if row whose block the given row belongs to, or null.
        public DbScriptIfRow? EnclosingIf(DbScriptRow row)
        {
            var membership = ComputeMembership();
            var idx = Rows.IndexOf(row);
            if (idx < 0 || (Rows[idx] is not DbScriptIfRow && !membership[idx]))
                return null;
            for (var i = idx; i >= 0; i--)
                if (Rows[i] is DbScriptIfRow ifRow)
                    return ifRow;
            return null;
        }

        // Clears stale InIf flags (rows flagged but not actually in a block, e.g. after their if
        // row was deleted), so a later if row dropped above them doesn't silently capture them.
        // Call inside a bulk edit from every structural mutation — flag changes are undoable.
        public void NormalizeIfMembership()
        {
            var membership = ComputeMembership();
            for (var i = 0; i < Rows.Count; i++)
                if (Rows[i].InIf && !membership[i] && Rows[i] is not DbScriptIfRow)
                    Rows[i].InIf = false;
        }

        private bool loading;

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (DbScriptRow row in e.NewItems)
                {
                    row.InIfChanged += OnRowInIfChanged;
                    if (row is DbScriptWaitRow wait)
                        wait.Duration.OnValueChanged += OnWaitDurationChanged;
                    else if (row is DbScriptIfRow ifRow)
                        ifRow.ConditionId.OnValueChanged += OnIfConditionChanged;
                }
            if (e.OldItems != null)
                foreach (DbScriptRow row in e.OldItems)
                {
                    row.InIfChanged -= OnRowInIfChanged;
                    if (row is DbScriptWaitRow wait)
                        wait.Duration.OnValueChanged -= OnWaitDurationChanged;
                    else if (row is DbScriptIfRow ifRow)
                        ifRow.ConditionId.OnValueChanged -= OnIfConditionChanged;
                }
            if (!loading)
                SyncDerived();
        }

        private void OnWaitDurationChanged(ParameterValueHolder<long> holder, long old, long @new) =>
            SyncDerived();

        private void OnIfConditionChanged(ParameterValueHolder<long> holder, long old, long @new) =>
            SyncDerived();

        private void OnRowInIfChanged(DbScriptRow row, bool old, bool @new) =>
            SyncDerived();
    }
}
