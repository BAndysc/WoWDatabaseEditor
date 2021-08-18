using System;
using Avalonia.Controls;
using Avalonia.Input;
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
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // down arrow focus first element
        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down)
                return;

            if (DataContext is SmartSelectViewModel vm)
            {
                vm.SelectFirstVisible();
                ListBox? listBox = this.FindControl<ListBox>("ListBox");
                if (listBox != null)
                    FocusManager.Instance.Focus(listBox.ItemContainerGenerator.ContainerFromIndex(listBox.SelectedIndex), NavigationMethod.Tab);
                e.Handled = true;
            }
        }
    }
}