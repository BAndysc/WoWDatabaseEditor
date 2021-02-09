using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.WPF.Editor.Views.Editing
{
    /// <summary>
    ///     Interaction logic for ParameterEditorView
    /// </summary>
    public class ParameterEditorView : Control
    {
        static ParameterEditorView()
        {
            FocusableProperty.OverrideMetadata(typeof(ParameterEditorView), new FrameworkPropertyMetadata(false));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ParameterEditorView), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ParameterEditorView), new FrameworkPropertyMetadata(typeof(ParameterEditorView)));
        }
    }
}