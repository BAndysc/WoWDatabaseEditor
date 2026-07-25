using System.ComponentModel;
using WDE.Common.Disposables;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // A standalone "// comment" row — a first-class editor entity (selectable, draggable,
    // copyable) wrapping a DbScriptCommentRow. Only squashed into the next action's comment
    // column on save.
    public class DbScriptCommentViewModel : DbScriptRowViewModel
    {
        public DbScriptCommentRow Comment { get; }

        public DbScriptCommentViewModel(DbScriptCommentRow comment) : base(comment)
        {
            Comment = comment;
            comment.Text.PropertyChanged += OnTextChanged;
            AutoDispose(new ActionDisposable(() => comment.Text.PropertyChanged -= OnTextChanged));
        }

        private void OnTextChanged(object? sender, PropertyChangedEventArgs e) =>
            RaisePropertyChanged(nameof(Text));

        public string Text => "// " + Comment.Text.Value;
    }
}
