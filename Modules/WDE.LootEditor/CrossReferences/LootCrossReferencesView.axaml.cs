using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.LootEditor.CrossReferences;

public partial class LootCrossReferencesView : UserControl
{
    public LootCrossReferencesView()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}