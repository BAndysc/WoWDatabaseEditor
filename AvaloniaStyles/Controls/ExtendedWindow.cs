using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;

namespace AvaloniaStyles.Controls
{
    public class ExtendedWindow : Window, IStyleable
    {
        public static readonly StyledProperty<IImage> ManagedIconProperty =
            AvaloniaProperty.Register<ExtendedWindow, IImage>(nameof(ManagedIcon));
        
        public static readonly StyledProperty<ExtendedWindowChrome> ChromeProperty =
            AvaloniaProperty.Register<ExtendedWindow, ExtendedWindowChrome>(nameof(Chrome));
        
        public static readonly StyledProperty<ToolBar> ToolBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, ToolBar>(nameof(ToolBar));

        public static readonly StyledProperty<IControl?> SideBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, IControl?>(nameof(SideBar));
        
        public static readonly StyledProperty<StatusBar> StatusBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, StatusBar>(nameof(StatusBar));
        
        public static readonly StyledProperty<TabStrip> TabStripProperty =
            AvaloniaProperty.Register<ExtendedWindow, TabStrip>(nameof(TabStrip));
        
        public static readonly StyledProperty<IControl> OverlayProperty =
            AvaloniaProperty.Register<ExtendedWindow, IControl>(nameof(Overlay));
        
        public IImage ManagedIcon
        {
            get => GetValue(ManagedIconProperty);
            set => SetValue(ManagedIconProperty, value);
        }
        
        public ExtendedWindowChrome Chrome
        {
            get => GetValue(ChromeProperty);
            set => SetValue(ChromeProperty, value);
        }
        
        public ToolBar ToolBar
        {
            get => GetValue(ToolBarProperty);
            set => SetValue(ToolBarProperty, value);
        }
        
        public IControl? SideBar
        {
            get => GetValue(SideBarProperty);
            set => SetValue(SideBarProperty, value);
        }
        
        public StatusBar StatusBar
        {
            get => GetValue(StatusBarProperty);
            set => SetValue(StatusBarProperty, value);
        }
        
        public IControl Overlay
        {
            get => GetValue(OverlayProperty);
            set => SetValue(OverlayProperty, value);
        }
        
        public TabStrip TabStrip
        {
            get => GetValue(TabStripProperty);
            set => SetValue(TabStripProperty, value);
        }
        
        Type IStyleable.StyleKey => typeof(ExtendedWindow);
        
        static ExtendedWindow()
        {
            IsActiveProperty.Changed.AddClassHandler<Window>((w, _) =>
            {
                if (!SystemTheme.CustomScalingValue.HasValue)
                    return;
                UpdateScaling(w);
            });
            IsActiveProperty.Changed.AddClassHandler<ExtendedWindow>((x, _) =>
                x.PseudoClasses.Set(":focused", x.IsActive));
            ToolBarProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-toolbar"));
            SideBarProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-sidebar"));
            StatusBarProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-statusbar"));
            TabStripProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-tabstrip"));
            OverlayProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-overlay"));

            WindowStateProperty.Changed.AddClassHandler<ExtendedWindow>((window, state) =>
            {
                window.PseudoClasses.Set(":maximized", (WindowState)state.NewValue! == WindowState.Maximized);
                window.PseudoClasses.Set(":fullscreen", (WindowState)state.NewValue == WindowState.FullScreen);
            });

            ChromeProperty.Changed.AddClassHandler<ExtendedWindow>((window, state) =>
            {
                if (state.NewValue is ExtendedWindowChrome b)
                    window.UpdateChromeHints(b);
            });
        }

        private void UpdateChromeHints(ExtendedWindowChrome chrome)
        {
            var useSystemChrome = chrome == ExtendedWindowChrome.AlwaysSystemChrome ||
                                  (chrome == ExtendedWindowChrome.MacOsChrome &&
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            
            if (useSystemChrome) 
            {
                ExtendClientAreaChromeHints |= ExtendClientAreaChromeHints.SystemChrome;
                ExtendClientAreaChromeHints &= ~ExtendClientAreaChromeHints.NoChrome;
            }
            else
            {
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            }
        }

        public ExtendedWindow()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                PseudoClasses.Add(":macos");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                PseudoClasses.Add(":windows");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                PseudoClasses.Add(":linux");
            SystemTheme.CustomScalingUpdated += SystemThemeOnCustomScalingUpdated;
        }

        private static void UpdateScaling(Window window)
        {
            double scaling = 1;
            if (!SystemTheme.CustomScalingValue.HasValue)
            {
                var primaryScreen = window.Screens.Primary ?? window.Screens.All.FirstOrDefault();
                scaling = (primaryScreen?.PixelDensity ?? 1);
            }
            else
                scaling = SystemTheme.CustomScalingValue.Value;
            
            var impl = window.PlatformImpl;
            var f = impl.GetType().GetField("_scaling", BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null)
            {
                var curVal = (double)f.GetValue(impl)!;
                if (Math.Abs(curVal - scaling) > 0.01)
                {
                    f.SetValue(impl, scaling);
                    impl.ScalingChanged(scaling);
                }
            }
        }

        private void SystemThemeOnCustomScalingUpdated(double? obj)
        {
            UpdateScaling(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            SystemTheme.CustomScalingUpdated -= SystemThemeOnCustomScalingUpdated;
            base.OnClosed(e);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            ExtendClientAreaChromeHints |= ExtendClientAreaChromeHints.OSXThickTitleBar;
            UpdateChromeHints(Chrome);

            if (SideBar == null && e.NameScope.Find("SidebarGrid") is Grid grid)
            {
                grid.ColumnDefinitions[0].MaxWidth = 0;
                grid.ColumnDefinitions[0].MinWidth = 0;
                grid.ColumnDefinitions[1].MaxWidth = 0;
                grid.ColumnDefinitions[1].MinWidth = 0;
            }
            else if (SideBar == null && e.NameScope.Find("ReversedSidebarGrid") is Grid grid2)
            {
                grid2.ColumnDefinitions[2].MaxWidth = 0;
                grid2.ColumnDefinitions[2].MinWidth = 0;
                grid2.ColumnDefinitions[1].MaxWidth = 0;
                grid2.ColumnDefinitions[1].MinWidth = 0;
            }
            if (SideBar == null && e.NameScope.Find("MainSplitter") is GridSplitter gridSplitter)
            {
                gridSplitter.MaxWidth = gridSplitter.MinWidth = gridSplitter.Width = 0;
            }
        }

        private void ContentChanged(AvaloniaPropertyChangedEventArgs e, string pseudoclass)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
                PseudoClasses.Remove(pseudoclass);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
                PseudoClasses.Add(pseudoclass);
            }
        }

        public void MaximizeNormalize()
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }
    }

    public enum ExtendedWindowChrome
    {
        NoSystemChrome,
        AlwaysSystemChrome,
        MacOsChrome
    }
}