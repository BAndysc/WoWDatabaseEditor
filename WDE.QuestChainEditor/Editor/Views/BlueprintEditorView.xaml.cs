using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WDE.QuestChainEditor.Editor.ViewModels;

namespace WDE.QuestChainEditor.Editor.Views
{
    /// <summary>
    ///     Interaction logic for BlueprintEditorView.xaml
    /// </summary>
    public partial class BlueprintEditorView : UserControl
    {
        private Point originalContentMouseDownPoint;

        public BlueprintEditorView()
        {
            InitializeComponent();
        }

        private QuestChainEditorViewModel ViewModel => (QuestChainEditorViewModel) DataContext;

        private void OnGraphControlMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ZoomAndPanControl.ZoomAboutPoint(ZoomAndPanControl.ContentScale + e.Delta / 1000.0f, e.GetPosition(GraphControl));

            e.Handled = true;
        }

        private void OnGraphControlRightMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            originalContentMouseDownPoint = e.GetPosition(GraphControl);
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
                Vector dragOffset = currentContentMousePoint - originalContentMouseDownPoint;

                ZoomAndPanControl.ContentOffsetX -= dragOffset.X;
                ZoomAndPanControl.ContentOffsetY -= dragOffset.Y;

                e.Handled = true;
            }
        }
    }
}