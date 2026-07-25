using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using TheEngine;
using WDE.WorldMap.Extensions;

namespace WDE.MapRenderer
{
    public partial class GameView : UserControl
    {
        private Control? enginePanel;

        public GameView()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => EnsureEnginePanel();
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // The engine host control is built in code: the "3D view" settings page picks between the
        // native child window panel and the composition-surface panel. Both expose the same
        // Game property and input surface.
        private void EnsureEnginePanel()
        {
            if (enginePanel != null || DataContext is not GameViewModel vm)
                return;

            Control panel;
            if (vm.UseCompositionEnginePanel)
            {
                var proper = new ProperTheEnginePanel();
                proper.Bind(ProperTheEnginePanel.GameProperty, new Binding(nameof(GameViewModel.CurrentGame)));
                panel = proper;
            }
            else
            {
                var native = new NativeTheEnginePanel();
                native.Bind(NativeTheEnginePanel.GameProperty, new Binding(nameof(GameViewModel.CurrentGame)));
                panel = native;
            }

            panel.Name = "TheEnginePanel";
            panel.Focusable = true;
            panel.PointerPressed += TheEnginePanel_OnPointerPressed;
            panel.PointerReleased += TheEnginePanel_OnPointerReleased;
            panel.PointerMoved += TheEnginePanel_OnPointerMoved;
            panel.ContextRequested += TheEnginePanel_OnContextRequested;

            enginePanel = panel;
            this.GetControl<Panel>("EnginePanelHost").Children.Add(panel);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            EnsureEnginePanel();
            DispatcherTimer.RunOnce(() =>
            {
                enginePanel?.Focus();
            }, TimeSpan.FromMilliseconds(1));
            if (enginePanel != null)
                enginePanel.ContextMenu = new ContextMenu();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            // workaround for https://github.com/AvaloniaUI/Avalonia/issues/8214 (confirmed)
            if (enginePanel != null)
                enginePanel.ContextMenu = null;
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
                else if (enginePanel?.ContextMenu != null)
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
