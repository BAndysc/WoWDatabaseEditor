using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.QuestChainEditor.Views;

public partial class QuestNodeView : UserControl
{
    public QuestNodeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}