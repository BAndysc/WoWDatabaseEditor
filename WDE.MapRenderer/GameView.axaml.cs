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