using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GeminiGraphEditor;
using WDE.QuestChainEditor.Editor.ViewModels;

namespace WDE.QuestChainEditor.Editor.Views
{
    /// <summary>
    ///     Interaction logic for BlueprintGraphControlView.xaml
    /// </summary>
    public partial class BlueprintGraphControlView : UserControl
    {
        private bool dragging;
        private bool hadMouseDown;
        private Point originalContentMouseDownPoint;

        private bool previouslyHadSelected;

        public BlueprintGraphControlView()
        {
            InitializeComponent();
        }

        private GraphViewModel ViewModel => (GraphViewModel) DataContext;

        private void OnGraphControlConnectionDragStarted(object sender, ConnectionDragStartedEventArgs e)
        {
            dragging = true;
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
            dragging = false;
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

        private void GraphControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            hadMouseDown = true;
            previouslyHadSelected = ViewModel.SelectedElements.Count() > 0 || ViewModel.SelectedConnections.Count() > 0;
        }

        private void GraphControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!dragging && hadMouseDown && !previouslyHadSelected && ViewModel.SelectedElements.Count() == 0 &&
                ViewModel.SelectedConnections.Count() == 0)
            {
                Point currentDragPoint = Mouse.GetPosition(GraphControl);
                ViewModel.ShowPicker(currentDragPoint);
            }

            previouslyHadSelected = false;
            hadMouseDown = false;
        }
    }
}