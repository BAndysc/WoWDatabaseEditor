using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    /// <summary>
    ///     Interaction logic for SmartScriptEditorView
    /// </summary>
    public partial class SmartScriptEditorView : UserControl
    {
        public SmartScriptEditorView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void DeselectAll(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
                e.ClickCount == 1 &&
                DataContext is SmartScriptEditorViewModel vm)
            {
                vm.DeselectAll.Execute();
                e.Handled = true;
            }
        }
    }
}