using System.Windows.Input;
using Avalonia;
using WDE.Common.Utils;

namespace WDE.DbScriptsEditor.Avalonia.Editor.UserControls
{
    // A "⏱ wait Ns" row — an equal row entity: click selects, Ctrl/Shift multi-select,
    // drag reorders, double-click edits the duration.
    public class DbScriptWaitView : SelectableTemplatedControl
    {
        public static readonly AvaloniaProperty EditCommandProperty =
            AvaloniaProperty.Register<DbScriptWaitView, ICommand>(nameof(EditCommand));

        public ICommand EditCommand
        {
            get => (ICommand?) GetValue(EditCommandProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(EditCommandProperty, value);
        }

        protected override void OnEdit()
        {
            EditCommand?.Execute(DataContext);
        }
    }
}
