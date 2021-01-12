using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeminiGraphEditor
{
    public class ConnectionItem : ListBoxItem
    {
        private bool isDragging;
        private bool isLeftMouseButtonDown;
        private Point lastMousePosition;

        static ConnectionItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionItem), new FrameworkPropertyMetadata(typeof(ConnectionItem)));
        }

        private GraphControl ParentGraphControl => VisualTreeUtility.FindParent<GraphControl>(this);

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DoSelection();

            e.Handled = true;

            base.OnMouseLeftButtonDown(e);
        }

        private void DoSelection()
        {
            GraphControl parentGraphControl = ParentGraphControl;
            if (parentGraphControl == null)
                return;

            parentGraphControl.SelectedConnections.Clear();
            IsSelected = true;
        }
    }
}