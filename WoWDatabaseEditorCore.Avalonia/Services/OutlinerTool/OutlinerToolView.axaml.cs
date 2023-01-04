using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Controls;
using WoWDatabaseEditorCore.Services.OutlinerTool;

namespace WoWDatabaseEditorCore.Avalonia.Services.OutlinerTool;

public partial class OutlinerToolView : UserControl
{
    private VirtualizedTreeView treeView;
    
    public OutlinerToolView()
    {
        InitializeComponent();
        treeView = this.GetControl<VirtualizedTreeView>("TreeView");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (treeView.SelectedNode is OutlinerItemViewModel vm && DataContext is OutlinerToolViewModel dataContext)
        {
            dataContext.Open(vm);
        }
    }
}
