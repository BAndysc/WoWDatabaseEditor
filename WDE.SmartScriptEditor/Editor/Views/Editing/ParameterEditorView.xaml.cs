using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.SmartScriptEditor.Editor.Views.Editing
{
    /// <summary>
    ///     Interaction logic for ParameterEditorView
    /// </summary>
    public partial class ParameterEditorView : UserControl
    {
        static ParameterEditorView()
        {
            FocusableProperty.OverrideMetadata(typeof(ParameterEditorView), new FrameworkPropertyMetadata(false));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(ParameterEditorView), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
        }
        
        public ParameterEditorView()
        {
            InitializeComponent();
        }
    }
}