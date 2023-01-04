using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DatabaseEditors.Avalonia.Views.OneToOneForeignKey;

public partial class OneToOneForeignKeyToolBar : UserControl
{
    public OneToOneForeignKeyToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}