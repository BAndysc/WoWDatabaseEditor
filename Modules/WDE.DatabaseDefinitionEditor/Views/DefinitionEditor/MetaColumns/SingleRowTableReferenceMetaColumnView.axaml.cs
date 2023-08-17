using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.DatabaseDefinitionEditor.Views.DefinitionEditor.MetaColumns;

public partial class SingleRowTableReferenceMetaColumnView : UserControl
{
    public SingleRowTableReferenceMetaColumnView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}