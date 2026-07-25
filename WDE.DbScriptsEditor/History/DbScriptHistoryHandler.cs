using System;
using System.Collections.Specialized;
using WDE.Common.History;
using WDE.Common.Utils;
using WDE.DbScriptsEditor.Models;
using WDE.Parameters.Models;

namespace WDE.DbScriptsEditor.History
{
    // Records every mutation of an editable dbscript for undo/redo: row add/remove/reorder (of
    // any row kind — action, wait or comment), command changes and every parameter/column edit.
    // Delay is NOT tracked: it is derived from the wait rows (undoing a row change re-derives it).
    // Mirrors EventAiHistoryHandler.
    public class DbScriptHistoryHandler : HistoryHandler, IDisposable
    {
        private readonly EditableDbScript script;

        public DbScriptHistoryHandler(EditableDbScript script)
        {
            this.script = script;
            script.Rows.CollectionChanged += OnRowsChanged;
            script.BulkEditingStarted += OnBulkStarted;
            script.BulkEditingFinished += OnBulkFinished;
            foreach (var row in script.Rows)
                Bind(row);
        }

        public void Dispose()
        {
            script.Rows.CollectionChanged -= OnRowsChanged;
            script.BulkEditingStarted -= OnBulkStarted;
            script.BulkEditingFinished -= OnBulkFinished;
            foreach (var row in script.Rows)
                Unbind(row);
        }

        private void OnBulkStarted() => StartBulkEdit();
        private void OnBulkFinished(string name) => EndBulkEdit(name.RemoveTags());

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var index = e.NewStartingIndex;
                foreach (DbScriptRow row in e.NewItems!)
                {
                    PushAction(new RowAddedAction(script, row, index++));
                    Bind(row);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (DbScriptRow row in e.OldItems!)
                {
                    Unbind(row);
                    PushAction(new RowRemovedAction(script, row, e.OldStartingIndex));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                PushAction(new RowMovedAction(script, e.OldStartingIndex, e.NewStartingIndex));
            }
        }

        private void Bind(DbScriptRow row)
        {
            row.BulkEditingStarted += OnBulkStarted;
            row.BulkEditingFinished += OnBulkFinished;
            row.InIfChanged += OnInIfChanged; // block membership is editor state like row order
            switch (row)
            {
                case EditableDbScriptStep step:
                    step.CommandChanged += OnCommandChanged;
                    foreach (var h in step.AllLongHolders)
                        // delay and condition_id are derived (waits / if rows), not undoable
                        if (!ReferenceEquals(h, step.Delay) && !ReferenceEquals(h, step.ConditionId))
                            h.OnValueChanged += OnLongChanged;
                    foreach (var h in step.AllFloatHolders)
                        h.OnValueChanged += OnFloatChanged;
                    break;
                case DbScriptWaitRow wait:
                    wait.Duration.OnValueChanged += OnLongChanged;
                    break;
                case DbScriptCommentRow comment:
                    comment.Text.OnValueChanged += OnStringChanged;
                    break;
                case DbScriptIfRow ifRow:
                    ifRow.ConditionId.OnValueChanged += OnLongChanged;
                    break;
            }
        }

        private void Unbind(DbScriptRow row)
        {
            row.BulkEditingStarted -= OnBulkStarted;
            row.BulkEditingFinished -= OnBulkFinished;
            row.InIfChanged -= OnInIfChanged;
            switch (row)
            {
                case EditableDbScriptStep step:
                    step.CommandChanged -= OnCommandChanged;
                    foreach (var h in step.AllLongHolders)
                        if (!ReferenceEquals(h, step.Delay) && !ReferenceEquals(h, step.ConditionId))
                            h.OnValueChanged -= OnLongChanged;
                    foreach (var h in step.AllFloatHolders)
                        h.OnValueChanged -= OnFloatChanged;
                    break;
                case DbScriptWaitRow wait:
                    wait.Duration.OnValueChanged -= OnLongChanged;
                    break;
                case DbScriptCommentRow comment:
                    comment.Text.OnValueChanged -= OnStringChanged;
                    break;
                case DbScriptIfRow ifRow:
                    ifRow.ConditionId.OnValueChanged -= OnLongChanged;
                    break;
            }
        }

        private void OnInIfChanged(DbScriptRow row, bool old, bool @new) =>
            PushAction(new InIfChangedAction(row, @new));

        private class InIfChangedAction : IHistoryAction
        {
            private readonly DbScriptRow row;
            private readonly bool @new;

            public InIfChangedAction(DbScriptRow row, bool @new)
            {
                this.row = row;
                this.@new = @new;
            }

            public string GetDescription() => @new ? "Moved row into if block" : "Moved row out of if block";
            public void Redo() => row.InIf = @new;
            public void Undo() => row.InIf = !@new;
        }

        private void OnLongChanged(ParameterValueHolder<long> h, long old, long @new) =>
            PushAction(new GenericParameterChangedAction<long>(h, old, @new));

        private void OnFloatChanged(ParameterValueHolder<float> h, float old, float @new) =>
            PushAction(new GenericParameterChangedAction<float>(h, old, @new));

        private void OnStringChanged(ParameterValueHolder<string> h, string old, string @new) =>
            PushAction(new GenericParameterChangedAction<string>(h, old, @new));

        private void OnCommandChanged(EditableDbScriptStep step, uint old, uint @new) =>
            PushAction(new CommandChangedAction(step, old, @new));

        private static string DescribeRow(DbScriptRow row) => row switch
        {
            EditableDbScriptStep step => step.Readable.RemoveTags(),
            DbScriptWaitRow wait => $"wait {wait.Duration.Value} ms",
            DbScriptCommentRow comment => $"comment '{comment.Text.Value}'",
            DbScriptIfRow ifRow => $"if (condition {ifRow.ConditionId.Value})",
            _ => "row",
        };

        private class RowAddedAction : IHistoryAction
        {
            private readonly EditableDbScript script;
            private readonly DbScriptRow row;
            private readonly int index;
            private readonly string readable;

            public RowAddedAction(EditableDbScript script, DbScriptRow row, int index)
            {
                this.script = script;
                this.row = row;
                this.index = index;
                readable = DescribeRow(row);
            }

            public string GetDescription() => "Added " + readable;
            public void Redo() => script.Rows.Insert(index, row);
            public void Undo() => script.Rows.Remove(row);
        }

        private class RowRemovedAction : IHistoryAction
        {
            private readonly EditableDbScript script;
            private readonly DbScriptRow row;
            private readonly int index;

            public RowRemovedAction(EditableDbScript script, DbScriptRow row, int index)
            {
                this.script = script;
                this.row = row;
                this.index = index;
            }

            public string GetDescription() => "Removed " + DescribeRow(row);
            public void Redo() => script.Rows.Remove(row);
            public void Undo() => script.Rows.Insert(index, row);
        }

        private class RowMovedAction : IHistoryAction
        {
            private readonly EditableDbScript script;
            private readonly int from;
            private readonly int to;

            public RowMovedAction(EditableDbScript script, int from, int to)
            {
                this.script = script;
                this.from = from;
                this.to = to;
            }

            public string GetDescription() => "Reordered rows";
            public void Redo() => script.Rows.Move(from, to);
            public void Undo() => script.Rows.Move(to, from);
        }

        private class CommandChangedAction : IHistoryAction
        {
            private readonly EditableDbScriptStep step;
            private readonly uint old;
            private readonly uint @new;

            public CommandChangedAction(EditableDbScriptStep step, uint old, uint @new)
            {
                this.step = step;
                this.old = old;
                this.@new = @new;
            }

            public string GetDescription() => "Changed command";
            public void Redo() => step.CommandId = @new;
            public void Undo() => step.CommandId = old;
        }

        private class GenericParameterChangedAction<T> : IHistoryAction where T : notnull
        {
            private readonly ParameterValueHolder<T> param;
            private readonly T old;
            private readonly T @new;

            public GenericParameterChangedAction(ParameterValueHolder<T> param, T old, T @new)
            {
                this.param = param;
                this.old = old;
                this.@new = @new;
            }

            public string GetDescription() => $"Changed {param.Name}";
            public void Redo() => param.Value = @new;
            public void Undo() => param.Value = old;
        }
    }
}
