using Avalonia;
using Avalonia.Controls.Primitives;
using AvaloniaStyles.Controls;
using WDE.DbScriptsEditor.Editor.ViewModels.Editing;

namespace WDE.DbScriptsEditor.Avalonia.Editor.Views.Editing
{
    // Copy-adapted from WDE.EventAiEditor.Avalonia: one parameter row of the edit dialog.
    // The template (Themes/Generic.axaml) picks a completion combo box / flags combo / checkbox /
    // plain value editor via ParameterDataTemplateSelector.
    public class ParameterEditorView : TemplatedControl
    {
        public static readonly AttachedProperty<bool> OnEnterPressedProperty =
            AvaloniaProperty.RegisterAttached<CompletionComboBox, bool>("OnEnterPressed", typeof(ParameterEditorView));

        static ParameterEditorView()
        {
            OnEnterPressedProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                box.OnEnterPressed += (sender, pressedArgs) =>
                {
                    var completionBox = (CompletionComboBox)sender!;
                    if (pressedArgs.SelectedItem == null && long.TryParse(pressedArgs.SearchText, out var l))
                    {
                        string name = "(unknown)";
                        if (completionBox.DataContext is EditableParameterViewModel<long> editableParam)
                            name = editableParam.Parameter.Parameter.ToString(l);
                        completionBox.SelectedItem = new ParameterOption(l, name);
                        pressedArgs.Handled = true;
                    }
                };
            });
        }

        public static bool GetOnEnterPressed(AvaloniaObject obj)
        {
            return (bool?)obj.GetValue(OnEnterPressedProperty) ?? false;
        }

        public static void SetOnEnterPressed(AvaloniaObject obj, bool value)
        {
            obj.SetValue(OnEnterPressedProperty, value);
        }
    }
}
