using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WDE.Blueprints.GeminiGraphEditor
{
    public class ConnectionItem : ListBoxItem
    {
        private Point _lastMousePosition;
        private bool _isLeftMouseButtonDown;
        private bool _isDragging;

        static ConnectionItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionItem),
                new FrameworkPropertyMetadata(typeof(ConnectionItem)));
        }
        
        private GraphControl ParentGraphControl
        {
            get { return VisualTreeUtility.FindParent<GraphControl>(this); }
        }
                
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DoSelection();

            e.Handled = true;

            base.OnMouseLeftButtonDown(e);
        }

        private void DoSelection()
        {
            var parentGraphControl = ParentGraphControl;
            if (parentGraphControl == null)
                return;

            parentGraphControl.SelectedConnections.Clear();
            IsSelected = true;
        }
    }
}
