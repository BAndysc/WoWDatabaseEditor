using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace WDE.QuestChainEditor.Views;

public partial class QuestChainDocumentToolBar : UserControl
{
    public QuestChainDocumentToolBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}