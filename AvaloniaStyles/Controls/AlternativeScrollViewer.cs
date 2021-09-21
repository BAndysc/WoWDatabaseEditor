using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;

namespace AvaloniaStyles.Controls
{
    public class AlternativeScrollViewer : ScrollViewer
    {
        private bool shouldHandleWheelEvent;
        public static readonly DirectProperty<AlternativeScrollViewer, bool> ShouldHandleWheelEventProperty = AvaloniaProperty.RegisterDirect<AlternativeScrollViewer, bool>("ShouldHandleWheelEvent", o => o.ShouldHandleWheelEvent, (o, v) => o.ShouldHandleWheelEvent = v);

        public bool ShouldHandleWheelEvent
        {
            get => shouldHandleWheelEvent;
            set => SetAndRaise(ShouldHandleWheelEventProperty, ref shouldHandleWheelEvent, value);
        }
    }
    
    public class AlternativeScrollContentPresenter : ScrollContentPresenter
    {
        private bool shouldHandleWheelEvent;
        public static readonly DirectProperty<AlternativeScrollContentPresenter, bool> ShouldHandleWheelEventProperty = AvaloniaProperty.RegisterDirect<AlternativeScrollContentPresenter, bool>("ShouldHandleWheelEvent", o => o.ShouldHandleWheelEvent, (o, v) => o.ShouldHandleWheelEvent = v);

        public bool ShouldHandleWheelEvent
        {
            get => shouldHandleWheelEvent;
            set => SetAndRaise(ShouldHandleWheelEventProperty, ref shouldHandleWheelEvent, value);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            e.Handled = shouldHandleWheelEvent;
        }
    }
}