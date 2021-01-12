using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GeminiGraphEditor;
using WDE.Blueprints.Editor.ViewModels;

namespace WDE.Blueprints.Editor.Views
{
    /// <summary>
    ///     Interaction logic for BlueprintGraphControlView.xaml
    /// </summary>
    public partial class BlueprintGraphControlView : UserControl
    {
        private Point originalContentMouseDownPoint;

        public BlueprintGraphControlView()
        {
            InitializeComponent();
        }

        private GraphViewModel ViewModel => (GraphViewModel) DataContext;

        private void OnGraphControlConnectionDragStarted(object sender, ConnectionDragStartedEventArgs e)
        {
            ConnectorViewModel sourceConnector = (ConnectorViewModel) e.SourceConnector.DataContext;
            Point currentDragPoint = Mouse.GetPosition(GraphControl);
            ConnectionViewModel connection = ViewModel.OnConnectionDragStarted(sourceConnector, currentDragPoint);
            e.Connection = connection;
        }

        private void OnGraphControlConnectionDragging(object sender, ConnectionDraggingEventArgs e)
        {
            Point currentDragPoint = Mouse.GetPosition(GraphControl);
            ConnectionViewModel connection = (ConnectionViewModel) e.Connection;
            ViewModel.OnConnectionDragging(currentDragPoint, connection);
        }

        private void OnGraphControlConnectionDragCompleted(object sender, ConnectionDragCompletedEventArgs e)
        {
            Point currentDragPoint = Mouse.GetPosition(GraphControl);
            ConnectorViewModel sourceConnector = (ConnectorViewModel) e.SourceConnector.DataContext;
            ConnectionViewModel newConnection = (ConnectionViewModel) e.Connection;
            ViewModel.OnConnectionDragCompleted(currentDragPoint, newConnection, sourceConnector);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            Focus();
            base.OnPreviewMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                ViewModel.DeleteSelectedConnections();
                ViewModel.DeleteSelectedElements();
            }

            base.OnKeyDown(e);
        }
    }
}