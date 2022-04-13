using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using TheEngine;

namespace WDE.MapRenderer
{
    public class GameView : UserControl
    {
        public GameView()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is GameViewModel gvm)
            {
                gvm.RequestDispose += RequestDispose;
            }
        }

        private void RequestDispose()
        {
            this.FindControl<TheEnginePanel>("TheEnginePanel").Dispose();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DispatcherTimer.RunOnce(() =>
            {
                this.FindControl<TheEnginePanel>("TheEnginePanel").Focus();
            }, TimeSpan.FromMilliseconds(1));
        }

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
        }
    }
}