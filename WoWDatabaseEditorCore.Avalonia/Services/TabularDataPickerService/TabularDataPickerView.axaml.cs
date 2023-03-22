using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;
using Prism.Commands;

namespace WoWDatabaseEditorCore.Avalonia.Services.TabularDataPickerService;

public partial class TabularDataPickerView : UserControl
{
    public TabularDataPickerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        KeyBindings.Add(new KeyBinding()
        {
            Command = new DelegateCommand(() =>
            {
                this.FindControl<TextBox>("SearchBox").Focus();
            }),
            Gesture = new KeyGesture(Key.F, AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>().CommandModifiers)
        });
    }

    // quality of life feature: arrow down in searchbox focuses first element
    private void SearchBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            VirtualizedGridView gridView = this.FindControl<VirtualizedGridView>("GridView");
            if (gridView == null)
                return;

            if (gridView.FocusedIndex == null || gridView.FocusedIndex == -1)
            {
                gridView.FocusedIndex = 0;
                gridView.Selection.Clear();
                if (gridView.Items.Count > 0)
                    gridView.Selection.Add(0);
            }
            gridView.Focus();

            e.Handled = true;
        }
    }
}