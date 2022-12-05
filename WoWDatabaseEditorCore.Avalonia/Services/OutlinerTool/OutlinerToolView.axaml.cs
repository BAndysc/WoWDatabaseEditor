using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using WoWDatabaseEditorCore.Services.OutlinerTool;

namespace WoWDatabaseEditorCore.Avalonia.Services.OutlinerTool;

public class OutlinerToolView : UserControl
{
    private OutlinerFastTreeView treeView;
    
    public OutlinerToolView()
    {
        InitializeComponent();
        treeView = this.FindControl<OutlinerFastTreeView>("TreeView");
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