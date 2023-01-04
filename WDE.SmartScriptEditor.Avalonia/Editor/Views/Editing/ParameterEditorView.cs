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

        public static readonly StyledProperty<bool> HoldsMultipleValuesProperty = AvaloniaProperty.Register<ParameterEditorView, bool>(nameof(HoldsMultipleValues));

        public bool HoldsMultipleValues
        {
            get => GetValue(HoldsMultipleValuesProperty);
            set => SetValue(HoldsMultipleValuesProperty, value);
        }

        static ParameterEditorView()
        {
            OnEnterPressedProperty.Changed.AddClassHandler<CompletionComboBox>((box, args) =>
            {
                box.OnEnterPressed += (sender, pressedArgs) =>
                {
                    var box = (CompletionComboBox)sender!;
                    if (pressedArgs.SelectedItem == null && long.TryParse(pressedArgs.SearchText, out var l))
                    {
                        string name = "(unknown)";
                        if (box.DataContext is EditableParameterViewModel<long> editableParam)
                            name = editableParam.Parameter.Parameter.ToString(l);
                        box.SelectedItem = new ParameterOption(l, name);
                        pressedArgs.Handled = true;
                    }
                };
            });
            
            HoldsMultipleValuesProperty.Changed.AddClassHandler<ParameterEditorView>((view, e) =>
            {
                view.Classes.Set("multiplevalues", (bool)e.NewValue!);
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