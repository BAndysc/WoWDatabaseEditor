using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;
using WDE.DbScriptsEditor.Editor.ViewModels;

namespace WDE.DbScriptsEditor.Avalonia.Editor.Views
{
    public partial class DbScriptSelectView : DialogViewBase
    {
        public DbScriptSelectView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // down arrow moves focus from the search box into the list
        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down)
                return;

            if (DataContext is DbScriptSelectViewModel vm)
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
