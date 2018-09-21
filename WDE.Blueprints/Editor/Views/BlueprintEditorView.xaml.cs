using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WDE.Blueprints.Editor.ViewModels;
using WDE.Blueprints.GeminiGraphEditor;

namespace WDE.Blueprints.Editor.Views
{
    /// <summary>
    /// Interaction logic for BlueprintEditorView.xaml
    /// </summary>
    public partial class BlueprintEditorView : UserControl
    {
        public BlueprintEditorView()
        {
            InitializeComponent();
        }

        private Point _originalContentMouseDownPoint;

        private GraphViewModel ViewModel
        {
            get { return ((BlueprintEditorViewModel)DataContext).GraphViewModel; }
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

        private void OnGraphControlRightMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            _originalContentMouseDownPoint = e.GetPosition(GraphControl);
            GraphControl.CaptureMouse();
            Mouse.OverrideCursor = Cursors.ScrollAll;
            e.Handled = true;
        }

        private void OnGraphControlRightMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.OverrideCursor = null;
            GraphControl.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnGraphControlMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed && GraphControl.IsMouseCaptured)
            {
                Point currentContentMousePoint = e.GetPosition(GraphControl);
                Vector dragOffset = currentContentMousePoint - _originalContentMouseDownPoint;

                ZoomAndPanControl.ContentOffsetX -= dragOffset.X;
                ZoomAndPanControl.ContentOffsetY -= dragOffset.Y;

                e.Handled = true;
            }
        }

        private void OnGraphControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ZoomAndPanControl.ZoomAboutPoint(
                ZoomAndPanControl.ContentScale + e.Delta / 1000.0f,
                e.GetPosition(GraphControl));

            e.Handled = true;
        }

        private void OnGraphControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.OnSelectionChanged();
        }

        private void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void OnGraphControlConnectionDragStarted(object sender, ConnectionDragStartedEventArgs e)
        {
            var sourceConnector = (ConnectorViewModel)e.SourceConnector.DataContext;
            var currentDragPoint = Mouse.GetPosition(GraphControl);
            var connection = ViewModel.OnConnectionDragStarted(sourceConnector, currentDragPoint);
            e.Connection = connection;
        }

        private void OnGraphControlConnectionDragging(object sender, ConnectionDraggingEventArgs e)
        {
            var currentDragPoint = Mouse.GetPosition(GraphControl);
            var connection = (ConnectionViewModel)e.Connection;
            ViewModel.OnConnectionDragging(currentDragPoint, connection);
        }

        private void OnGraphControlConnectionDragCompleted(object sender, ConnectionDragCompletedEventArgs e)
        {
            var currentDragPoint = Mouse.GetPosition(GraphControl);
            var sourceConnector = (ConnectorViewModel)e.SourceConnector.DataContext;
            var newConnection = (ConnectionViewModel)e.Connection;
            ViewModel.OnConnectionDragCompleted(currentDragPoint, newConnection, sourceConnector);
        }

        private void GraphControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }
    }
}
