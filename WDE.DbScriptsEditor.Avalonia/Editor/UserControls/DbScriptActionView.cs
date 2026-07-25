using System.Windows.Input;
using Avalonia;
using WDE.Common.Utils;

namespace WDE.DbScriptsEditor.Avalonia.Editor.UserControls
{
    // The dbscript ACTION row, rendered exactly like an EventAI/SmartScript action row.
    // Selection, drag and double-click behavior come from the shared base — actions, waits and
    // comments are equal row entities.
    public class DbScriptActionView : SelectableTemplatedControl
    {
        public static readonly AvaloniaProperty EditActionCommandProperty =
            AvaloniaProperty.Register<DbScriptActionView, ICommand>(nameof(EditActionCommand));

        public static readonly AvaloniaProperty DirectEditParameterProperty =
            AvaloniaProperty.Register<DbScriptActionView, ICommand>(nameof(DirectEditParameter));

        public ICommand EditActionCommand
        {
            get => (ICommand?) GetValue(EditActionCommandProperty) ?? AlwaysDisabledCommand.Command;
            set => SetValue(EditActionCommandProperty, value);
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
            EditActionCommand?.Execute(DataContext);
        }
    }
}
