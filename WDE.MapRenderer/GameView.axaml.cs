using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using TheEngine;
using WDE.WorldMap.Extensions;

namespace WDE.MapRenderer
{
    public class GameView : UserControl
    {
        private TheEnginePanel enginePanel;
        
        public GameView()
        {
            InitializeComponent();
            enginePanel = this.FindControl<TheEnginePanel>("TheEnginePanel");
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

        private bool canShowContextMenu = true;
        private Point initialTouch;

        private void TheEnginePanel_OnContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            e.Handled = !canShowContextMenu;
            if (canShowContextMenu)
            {
                var items = ((GameViewModel)DataContext!).CurrentGame!.GenerateContextMenu();
                if (items == null)
                    e.Handled = true;
                else
                {
                    enginePanel.ContextMenu!.Items = items.Select(i => new MenuItem()
                    {
                        Header = i.Item1,
                        Command = i.Item2
                    });  
                }
            }
        }

        private void TheEnginePanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            canShowContextMenu = true;
            initialTouch = e.GetPosition(this);
        }

        private void TheEnginePanel_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
        }

        private void TheEnginePanel_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (canShowContextMenu)
            {
                var nowTouch = e.GetPosition(this);
                if (nowTouch.Distance(initialTouch) > 2)
                    canShowContextMenu = false;
            }
        }
    }
}