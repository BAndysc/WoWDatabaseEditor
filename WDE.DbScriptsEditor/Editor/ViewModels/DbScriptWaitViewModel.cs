using System.ComponentModel;
using System.Globalization;
using WDE.Common.Disposables;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // A "⏱ wait Ns" row — a first-class editor entity (selectable, draggable, copyable) wrapping
    // a DbScriptWaitRow. The absolute time label is derived by the script from the row order.
    public class DbScriptWaitViewModel : DbScriptRowViewModel
    {
        public DbScriptWaitRow Wait { get; }

        public DbScriptWaitViewModel(DbScriptWaitRow wait) : base(wait)
        {
            Wait = wait;
            wait.PropertyChanged += OnWaitChanged;
            wait.Duration.PropertyChanged += OnDurationChanged;
            AutoDispose(new ActionDisposable(() =>
            {
                wait.PropertyChanged -= OnWaitChanged;
                wait.Duration.PropertyChanged -= OnDurationChanged;
            }));
        }

        private void OnWaitChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DbScriptWaitRow.StartsAt) or nameof(DbScriptWaitRow.EndsAt))
                RaisePropertyChanged(nameof(AbsoluteLabel));
        }

        private void OnDurationChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(Label));
            RaisePropertyChanged(nameof(AbsoluteLabel));
        }

        public string Label => $"⏱  wait {Format(Wait.Duration.Value)}";
        public string AbsoluteLabel => $"then at {Format(Wait.EndsAt)}";

        private static string Format(long ms)
        {
            var seconds = ms / 1000.0;
            return seconds.ToString("0.###", CultureInfo.InvariantCulture) + "s";
        }
    }
}
