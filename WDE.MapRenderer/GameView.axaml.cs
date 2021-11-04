using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TheEngine;
using WDE.Common.MPQ;

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

        private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
        }
    }
}