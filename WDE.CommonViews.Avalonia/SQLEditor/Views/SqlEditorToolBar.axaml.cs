using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.CommonViews.Avalonia.SQLEditor.Views;

public partial class SqlEditorToolBar : UserControl
{
    public SqlEditorToolBar()
    {
        InitializeComponent();
    }
        
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}