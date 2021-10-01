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

        private bool specialCopying;
        public static readonly DirectProperty<ParameterEditorView, bool> SpecialCopyingProperty = AvaloniaProperty.RegisterDirect<ParameterEditorView, bool>("SpecialCopying", o => o.SpecialCopying, (o, v) => o.SpecialCopying = v);

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

        public bool SpecialCopying
        {
            get => specialCopying;
            set => SetAndRaise(SpecialCopyingProperty, ref specialCopying, value);
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