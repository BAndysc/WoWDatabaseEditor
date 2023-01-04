using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using TheEngine;
using WDE.WorldMap.Extensions;

namespace WDE.MapRenderer
{
    public partial class GameView : UserControl
    {
        private Control enginePanel;
        
        public GameView()
        {
            InitializeComponent();
            enginePanel = this.GetControl<Control>("TheEnginePanel");
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DispatcherTimer.RunOnce(() =>
            {
                this.GetControl<Control>("TheEnginePanel").Focus();
            }, TimeSpan.FromMilliseconds(1));
            enginePanel.ContextMenu = new ContextMenu();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            // workaround for https://github.com/AvaloniaUI/Avalonia/issues/8214 (confirmed)
            enginePanel.ContextMenu = null;
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
                else if (enginePanel.ContextMenu != null)
                {
                    enginePanel.ContextMenu.ItemsSource = items.Select(i =>
                    {
                        if (i.Item1 == "-")
                            return (object)new Separator();

                        return new MenuItem()
                        {
                            Header = i.Item1,
                            Command = i.Item2,
                            CommandParameter = i.Item3!
                        };
                    }).ToList();
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