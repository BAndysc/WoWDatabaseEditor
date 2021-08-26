using Avalonia;
using Avalonia.Controls.Primitives;
using AvaloniaStyles.Controls;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views.Editing
{
    /// <summary>
    ///     Interaction logic for ParameterEditorView
    /// </summary>
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
                    var box = (CompletionComboBox)sender!;
                    if (pressedArgs.SelectedItem == null && long.TryParse(pressedArgs.SearchText, out var l))
                        box.SelectedItem = new ParameterOption(l, "(unknown)");
                };
            });
        }

        public static bool GetOnEnterPressed(IAvaloniaObject obj)
        {
            return obj.GetValue(OnEnterPressedProperty);
        }

        public static void SetOnEnterPressed(IAvaloniaObject obj, bool value)
        {
            obj.SetValue(OnEnterPressedProperty, value);
        }
    }
}