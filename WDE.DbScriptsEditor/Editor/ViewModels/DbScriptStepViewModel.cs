using System.ComponentModel;
using System.Linq;
using WDE.Common.Disposables;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Editor.ViewModels
{
    // An action row VM. Equal sibling of the wait/comment row VMs.
    public class DbScriptStepViewModel : DbScriptRowViewModel
    {
        public EditableDbScriptStep Step { get; }

        public DbScriptStepViewModel(EditableDbScriptStep step)
            : base(step)
        {
            Step = step;
            step.PropertyChanged += OnStepChanged;
            step.ConditionId.PropertyChanged += OnConditionChanged;
            AutoDispose(new ActionDisposable(() =>
            {
                step.PropertyChanged -= OnStepChanged;
                step.ConditionId.PropertyChanged -= OnConditionChanged;
            }));
        }

        private void OnStepChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(EditableDbScriptStep.CommandName) or nameof(EditableDbScriptStep.VariantName))
                RaisePropertyChanged(nameof(DisplayName));
            else if (e.PropertyName == nameof(EditableDbScriptStep.Readable))
                RaisePropertyChanged(nameof(Readable));
            else if (e.PropertyName == nameof(EditableDbScriptStep.FormattedReadable))
                RaisePropertyChanged(nameof(FormattedReadable));
            else if (e.PropertyName == nameof(EditableDbScriptStep.ReadableContext))
                RaisePropertyChanged(nameof(ReadableContext));
            else if (e.PropertyName == nameof(EditableDbScriptStep.UnusedColumns))
            {
                RaisePropertyChanged(nameof(HasUnusedColumnWarning));
                RaisePropertyChanged(nameof(UnusedColumnsText));
            }
        }

        private void OnConditionChanged(object? sender, PropertyChangedEventArgs e)
        {
            RefreshCondition();
        }

        /// <summary>The condition id changed — the editor listens on HasCondition to regroup.</summary>
        public void RefreshCondition()
        {
            RaisePropertyChanged(nameof(HasCondition));
        }

        // Inspection results for this step, assigned by the editor after each change.
        public System.Collections.ObjectModel.ObservableCollection<DbScriptDiagnostic> Diagnostics { get; } = new();
        public bool HasDiagnostics => Diagnostics.Count > 0;

        public void SetDiagnostics(System.Collections.Generic.IReadOnlyList<DbScriptDiagnostic> diagnostics)
        {
            Diagnostics.Clear();
            foreach (var d in diagnostics)
                Diagnostics.Add(d);
            RaisePropertyChanged(nameof(HasDiagnostics));
        }

        public string DisplayName => Step.VariantName ?? Step.CommandName;
        public string Readable => Step.Readable;
        public string FormattedReadable => Step.FormattedReadable;
        // FormattedTextBlock.ContextArray is IList<object>; the backing store is a List<object>.
        public System.Collections.Generic.IList<object>? ReadableContext =>
            Step.ReadableContext as System.Collections.Generic.IList<object>;

        public System.Collections.ObjectModel.ObservableCollection<DbScriptEditableParameter> UsedParameters => Step.UsedParameters;
        public System.Collections.ObjectModel.ObservableCollection<DbScriptEditableParameter> AdvancedParameters => Step.AdvancedParameters;

        public bool HasCondition => Step.ConditionId.Value > 0;

        public bool HasUnusedColumnWarning => Step.UnusedColumns.Count > 0;
        public string UnusedColumnsText =>
            $"⚠ nonzero unused column(s): {string.Join(", ", Step.UnusedColumns.Select(c => c.ToLowerInvariant()))}";
    }
}
