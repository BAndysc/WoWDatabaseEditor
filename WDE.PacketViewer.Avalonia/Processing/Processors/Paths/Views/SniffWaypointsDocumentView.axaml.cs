using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;
using WDE.PacketViewer.Processing.Processors.Paths.ViewModels;

namespace WDE.PacketViewer.Avalonia.Processing.Processors.Paths.Views;

public partial class SniffWaypointsDocumentView : UserControl
{
    public SniffWaypointsDocumentView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ExporterComboBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == CompletionComboBox.SelectedItemProperty)
        {
            if (DataContext is SniffWaypointsDocumentViewModel vm)
            {
                vm.UpdateQuery();
            }
        }
    }
}