using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GeminiGraphEditor
{
    public class ElementItem : ListBoxItem
    {
        private bool isDragging;
        private bool isLeftMouseButtonDown;
        private Point lastMousePosition;

        static ElementItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ElementItem), new FrameworkPropertyMetadata(typeof(ElementItem)));
        }

        private GraphControl ParentGraphControl => VisualTreeUtility.FindParent<GraphControl>(this);

        public void BringToFront()
        {
            GraphControl parentGraphControl = ParentGraphControl;
            if (parentGraphControl == null)
                return;

            int maxZ = parentGraphControl.GetMaxZIndex();
            ZIndex = maxZ + 1;
        }

        #region Dependency properties

        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X",
            typeof(double),
            typeof(ElementItem),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double X
        {
            get => (double) GetValue(XProperty);
            set => SetValue(XProperty, value);
        }

        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y",
            typeof(double),
            typeof(ElementItem),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double Y
        {
            get => (double) GetValue(YProperty);
            set => SetValue(YProperty, value);
        }

        public static readonly DependencyProperty ZIndexProperty = DependencyProperty.Register("ZIndex",
            typeof(int),
            typeof(ElementItem),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public int ZIndex
        {
            get => (int) GetValue(ZIndexProperty);
            set => SetValue(ZIndexProperty, value);
        }

        #endregion

        #region Mouse input

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            BringToFront();
            base.OnMouseDown(e);
        }

        private double startX;
        private double startY;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            DoSelection();

            GraphControl parentGraphControl = ParentGraphControl;
            if (parentGraphControl != null)
                lastMousePosition = e.GetPosition(parentGraphControl);

            startX = X;
            startY = Y;

            isLeftMouseButtonDown = true;

            e.Handled = true;

            base.OnMouseLeftButtonDown(e);
        }

        private void DoSelection()
        {
            GraphControl parentGraphControl = ParentGraphControl;
            if (parentGraphControl == null)
                return;

            parentGraphControl.SelectedElements.Clear();
            IsSelected = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newMousePosition = e.GetPosition(ParentGraphControl);
                Vector delta = newMousePosition - lastMousePosition;

                X = startX + delta.X; // delta.X;
                Y = startY + delta.Y; //

                X -= (X + ActualWidth / 2) % 25;
                Y -= Y % 25;

                // _lastMousePosition = newMousePosition;
            }

            if (isLeftMouseButtonDown)
            {
                isDragging = true;
                CaptureMouse();
            }

            e.Handled = true;

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (isLeftMouseButtonDown)
            {
                isLeftMouseButtonDown = false;

                if (isDragging)
                {
                    ReleaseMouseCapture();
                    isDragging = false;
                }
            }

            e.Handled = true;

            base.OnMouseLeftButtonUp(e);
        }

        #endregion
    }
}