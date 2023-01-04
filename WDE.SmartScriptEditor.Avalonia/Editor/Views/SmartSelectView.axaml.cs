using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Controls;
using WDE.SmartScriptEditor.Editor.ViewModels;

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
            var searchTextBox = this.GetControl<TextBox>("SearchTextBox");
            searchTextBox.AddHandler(KeyDownEvent, InputElement_OnKeyDown, RoutingStrategies.Tunnel);
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // down arrow focus first element
        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender!;
            if (!(e.Key == Key.Down || e.Key == Key.Right && textBox.SelectionStart == textBox.Text?.Length))
                return;

            if (DataContext is SmartSelectViewModel vm)
            {
                vm.SelectFirstVisible();
                ListBox? listBox = this.FindControl<ListBox>("ListBox");
                if (listBox != null)
                {
                    var index = listBox.SelectedIndex;
                    if (index < 0 || index >= listBox.ItemCount)
                        index = 0;
                    listBox.ContainerFromIndex(index)!.Focus(NavigationMethod.Tab);
                }
                e.Handled = true;
            }
        }
    }
}
