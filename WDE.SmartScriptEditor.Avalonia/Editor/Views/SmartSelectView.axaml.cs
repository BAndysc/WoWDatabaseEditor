using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    /// <summary>
    ///     Interaction logic for SmartSelectView.xaml
    /// </summary>
    public partial class SmartSelectView : DialogViewBase
    {
        public SmartSelectView()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        //
        // private void TextBox_KeyUp(object sender, KeyEventArgs e)
        // {
        //     if (e.Key == Key.Enter)
        //     {
        //         if (Items.Items.Count > 0)
        //         {
        //             if (Items.SelectedIndex == -1)
        //                 Items.SelectedIndex = 0;
        //
        //             if (DataContext is SmartSelectViewModel vm)
        //                 vm.Accept.Execute();
        //         }
        //     }
        // }
        //
        // private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        // {
        //     if (e.ClickCount == 2 && DataContext is SmartSelectViewModel vm)
        //         vm.Accept.Execute();
        // }
    }
}