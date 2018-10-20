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
using WDE.QuestChainEditor.Editor.ViewModels;
using GeminiGraphEditor;

namespace WDE.QuestChainEditor.Editor.Views
{
    /// <summary>
    /// Interaction logic for BlueprintGraphControlView.xaml
    /// </summary>
    public partial class BlueprintGraphControlView : UserControl
    {
        public BlueprintGraphControlView()
        {
            InitializeComponent();
        }

        private Point _originalContentMouseDownPoint;

        private GraphViewModel ViewModel
        {
            get { return (GraphViewModel)DataContext; }
        }

        private void OnGraphControlConnectionDragStarted(object sender, ConnectionDragStartedEventArgs e)
        {
            dragging = true;
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
            dragging = false;
            var currentDragPoint = Mouse.GetPosition(GraphControl);
            var sourceConnector = (ConnectorViewModel)e.SourceConnector.DataContext;
            var newConnection = (ConnectionViewModel)e.Connection;
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

        private bool previouslyHadSelected;
        private bool hadMouseDown;
        private bool dragging;

        private void GraphControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            hadMouseDown = true;
            previouslyHadSelected = (ViewModel.SelectedElements.Count() > 0 || ViewModel.SelectedConnections.Count() > 0);
        }

        private void GraphControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!dragging && hadMouseDown && !previouslyHadSelected && ViewModel.SelectedElements.Count() == 0 && ViewModel.SelectedConnections.Count() == 0)
            {
                var currentDragPoint = Mouse.GetPosition(GraphControl);
                ViewModel.ShowPicker(currentDragPoint);
            }
            previouslyHadSelected = false;
            hadMouseDown = false;
        }
    }
}
