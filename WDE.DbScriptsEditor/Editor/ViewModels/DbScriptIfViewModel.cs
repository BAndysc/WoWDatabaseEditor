using System;
using System.Collections.Generic;
using System.ComponentModel;
using WDE.Common.Disposables;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // An "if <condition>" row VM. An equal row entity like action/wait/comment: selectable,
    // reorderable, copyable, deletable. It stays even with zero member rows (exports nothing
    // then) until the user deletes it. Clicking the condition text / double-clicking opens the
    // cmangos condition tree editor.
    public class DbScriptIfViewModel : DbScriptRowViewModel
    {
        public DbScriptIfRow If { get; }

        private readonly Func<DbScriptIfRow, long, string?>? conditionReadable;

        public DbScriptIfViewModel(DbScriptIfRow ifRow, Func<DbScriptIfRow, long, string?>? conditionReadable = null)
            : base(ifRow)
        {
            If = ifRow;
            this.conditionReadable = conditionReadable;
            ifRow.ConditionId.PropertyChanged += OnConditionChanged;
            AutoDispose(new ActionDisposable(() => ifRow.ConditionId.PropertyChanged -= OnConditionChanged));
        }

        private void OnConditionChanged(object? sender, PropertyChangedEventArgs e) => RefreshText();

        public void RefreshText()
        {
            RaisePropertyChanged(nameof(FormattedText));
            RaisePropertyChanged(nameof(Context));
        }

        // The whole line is one clickable span opening the condition tree editor.
        public string FormattedText => $"[s=0]if {Escape(ReadableText)}[/s]";

        public IList<object> Context => new List<object> { new DbScriptConditionSlot(If) };

        private string ReadableText
        {
            get
            {
                if (If.ConditionId.Value == 0)
                    return "(no condition — click to set)";
                var readable = conditionReadable?.Invoke(If, If.ConditionId.Value);
                return string.IsNullOrEmpty(readable) ? $"condition {If.ConditionId.Value}" : readable!;
            }
        }

        // FormattedTextBlock uses '[' and '\\' as markup control characters.
        private static string Escape(string value) =>
            value.Replace("\\", "\\\\").Replace("[", "\\[");
    }
}
