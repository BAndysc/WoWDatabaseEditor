using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls.FastTableView;
using WDE.DatabaseDefinitionEditor.ViewModels;
using WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

namespace WDE.DatabaseDefinitionEditor.Views;

public partial class NonTemplateModePreview : UserControl
{
    public NonTemplateModePreview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is DefinitionEditorViewModel vm)
            vm.OnDataChanged += OnDataChanged;
    }

    private void OnDataChanged()
    {
        var TableView = this.Get<VeryFastTableView>("TableView");
        TableView.InvalidateVisual();
        TableView.InvalidateMeasure();
        TableView.InvalidateArrange();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is DefinitionEditorViewModel vm)
            vm.OnDataChanged -= OnDataChanged;
    }

    private void VeryFastTableView_OnColumnPressed(object? sender, ColumnPressedEventArgs e)
    {
        if (DataContext is DefinitionEditorViewModel vm && vm.SelectedTable != null)
        {
            vm.SelectedTable.SelectedColumnOrGroup = e.Column as ColumnViewModel;
        }
    }
}