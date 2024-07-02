using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Views
{
    /// <summary>
    ///     Interaction logic for SmartSelectView.xaml
    /// </summary>
    public partial class SmartSelectView : DialogViewBase, IDialogWindowActivatedListener
    {
        public SmartSelectView()
        {
            InitializeComponent();
            SearchTextBox.AddHandler(KeyDownEvent, InputElement_OnKeyDown, RoutingStrategies.Tunnel);
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
                if (ListBox != null)
                {
                    var index = ListBox.SelectedIndex;
                    if (index < 0 || index >= ListBox.ItemCount)
                        index = 0;
                    ListBox.ContainerFromIndex(index)!.Focus(NavigationMethod.Tab);
                }
                e.Handled = true;
            }
        }

        private bool isActivated;
        private IDisposable? gotFocusDisposable;

        public void OnActivated()
        {
            gotFocusDisposable = GotFocusEvent.AddClassHandler<InputElement>(OnGlobalGotFocus);
            isActivated = true;
            SearchTextBox.Focus(NavigationMethod.Tab);
        }

        private void OnGlobalGotFocus(InputElement arg1, GotFocusEventArgs arg2)
        {
            if (!isActivated)
                return;

            var parent = arg1.FindAncestorOfType<Window>(true);
            var thisParent = this.FindAncestorOfType<Window>(true);
            if (parent != thisParent && thisParent != null)
            {
                DispatcherTimer.RunOnce(() =>
                {
                    if (isActivated)
                        SearchTextBox.Focus(NavigationMethod.Tab);
                }, TimeSpan.FromMilliseconds(1));
            }
        }

        public void OnDeactivated()
        {
            gotFocusDisposable?.Dispose();
            isActivated = false;
        }
    }
}
