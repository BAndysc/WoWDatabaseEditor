using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.WPF.Editor.Views
{
    /// <summary>
    ///     Interaction logic for SmartSelectView.xaml
    /// </summary>
    public partial class SmartSelectView : UserControl
    {
        public SmartSelectView()
        {
            InitializeComponent();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Items.Items.Count > 0)
                {
                    if (Items.SelectedIndex == -1)
                        Items.SelectedIndex = 0;

                    if (DataContext is SmartSelectViewModel vm)
                        vm.Accept.Execute();
                }
            }
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && DataContext is SmartSelectViewModel vm)
                vm.Accept.Execute();
        }
    }
}