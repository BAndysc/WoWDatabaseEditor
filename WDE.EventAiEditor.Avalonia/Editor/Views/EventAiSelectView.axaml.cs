using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Controls;
using WDE.EventAiEditor.Editor.ViewModels;

namespace WDE.EventAiEditor.Avalonia.Editor.Views
{
    /// <summary>
    ///     Interaction logic for EventAiSelectView.xaml
    /// </summary>
    public partial class EventAiSelectView : DialogViewBase
    {
        public EventAiSelectView()
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

            if (DataContext is EventAiSelectViewModel vm)
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
