using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Prism.Commands;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public partial class DumpView : UserControl
{
    public DumpView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void TableItemKeyPressed(object? sender, KeyEventArgs e)
    {
        var tablesListBox = this.GetControl<ListBox>("TablesListBox");
        if (e.Key == Key.Space)
        {
            bool? setToValue = null;
            foreach (var selected in tablesListBox.Selection.SelectedItems.Cast<DumpTableViewModel>())
            {
                if (setToValue.HasValue)
                {
                    selected.IsChecked = setToValue.Value;
                }
                else
                {
                    selected.IsChecked = !selected.IsChecked;
                    setToValue = selected.IsChecked;
                }
            }

            e.Handled = true;
        }
    }
}