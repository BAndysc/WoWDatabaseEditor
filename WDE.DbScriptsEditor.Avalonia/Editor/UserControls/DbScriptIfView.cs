using System.Windows.Input;
using Avalonia;
using WDE.Common.Utils;

namespace WDE.DbScriptsEditor.Avalonia.Editor.UserControls
{
    // The "if <condition>" row — an equal row entity: click selects it, Ctrl/Shift multi-select,
    // drag reorders it, double-click (or clicking the condition text) opens the condition tree
    // editor. Behavior comes from the shared base like every other row kind.
    public class DbScriptIfView : SelectableTemplatedControl
    {
        public static readonly AvaloniaProperty EditCommandProperty =
            AvaloniaProperty.Register<DbScriptIfView, ICommand>(nameof(EditCommand));

        public static readonly AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<DbScriptIfView, ICommand>(nameof(DirectEditParameter));

        public ICommand EditCommand
        {
            get => (ICommand?) GetValue(EditCommandProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(EditCommandProperty, value);
        }

        public ICommand DirectEditParameter
        {
            get => (ICommand?) GetValue(DirectEditParameterProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(DirectEditParameterProperty, value);
        }

        protected override void OnDirectEdit(object context)
        {
            DirectEditParameter?.Execute(context);
        }

        protected override void OnEdit()
        {
            EditCommand?.Execute(DataContext);
        }
    }
}
