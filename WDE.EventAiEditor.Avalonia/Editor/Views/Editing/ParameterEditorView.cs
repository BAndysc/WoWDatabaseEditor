using Avalonia;
using Avalonia.Controls.Primitives;
using AvaloniaStyles.Controls;
using WDE.EventAiEditor.Editor.ViewModels.Editing;

namespace WDE.EventAiEditor.Avalonia.Editor.Views.Editing
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