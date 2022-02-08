using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using AvaloniaGraph.Controls;
using AvaloniaGraph.ViewModels;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views;

public class QuestChainDocumentView : UserControl
{
    private GraphControl graphControl = null!;
    private Canvas? elementsCanvas = null!;
    
    public QuestChainDocumentView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        graphControl = this.FindControl<GraphControl>("GraphControl");
        elementsCanvas = graphControl.FindDescendantOfType<Canvas>();
    }

    private QuestChainDocumentViewModel ViewModel => (DataContext as QuestChainDocumentViewModel)!;
    
    private void OnGraphControlConnectionDragStarted(object sender, ConnectionDragStartedEventArgs e)
    {
        elementsCanvas ??= graphControl.FindDescendantOfType<Canvas>();
        var sourceConnector = (ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>)e.SourceConnector.DataContext!;
        var currentDragPoint = e.GetPosition(elementsCanvas);
        var connection = ViewModel.OnConnectionDragStarted(sourceConnector, currentDragPoint);
        e.Connection = connection;
    }

    private void OnGraphControlConnectionDragging(object sender, ConnectionDraggingEventArgs e)
    {
        elementsCanvas ??= graphControl.FindDescendantOfType<Canvas>();
        var currentDragPoint = e.GetPosition(elementsCanvas);
        var connection = (QuestConnectionViewModel)e.Connection;
        ViewModel.OnConnectionDragging(currentDragPoint, connection);
    }

    private void OnGraphControlConnectionDragCompleted(object sender, ConnectionDragCompletedEventArgs e)
    {
        elementsCanvas ??= graphControl.FindDescendantOfType<Canvas>();
        var currentDragPoint = e.GetPosition(elementsCanvas);
        var sourceConnector = (ConnectorViewModel<QuestViewModel, QuestConnectionViewModel>)e.SourceConnector.DataContext!;
        var newConnection = (QuestConnectionViewModel)e.Connection;
        ViewModel.OnConnectionDragCompleted(currentDragPoint, newConnection, sourceConnector);
    }

}