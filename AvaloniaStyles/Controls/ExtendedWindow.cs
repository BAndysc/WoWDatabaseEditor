using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaStyles.Utils;
using Classic.Avalonia.Theme;

namespace AvaloniaStyles.Controls
{
    public class ExtendedWindow : ClassicWindow
    {
        public static readonly StyledProperty<IImage> ManagedIconProperty =
            AvaloniaProperty.Register<ExtendedWindow, IImage>(nameof(ManagedIcon));
        
        public static readonly StyledProperty<ExtendedWindowChrome> ChromeProperty =
            AvaloniaProperty.Register<ExtendedWindow, ExtendedWindowChrome>(nameof(Chrome));
        
        public static readonly StyledProperty<ToolBar> ToolBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, ToolBar>(nameof(ToolBar));

        public static readonly StyledProperty<Control?> SideBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, Control?>(nameof(SideBar));
        
        public static readonly StyledProperty<StatusBar> StatusBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, StatusBar>(nameof(StatusBar));
        
        public static readonly StyledProperty<TabStrip> TabStripProperty =
            AvaloniaProperty.Register<ExtendedWindow, TabStrip>(nameof(TabStrip));
        
        public static readonly StyledProperty<Control> OverlayProperty =
            AvaloniaProperty.Register<ExtendedWindow, Control>(nameof(Overlay));
        
        public static readonly StyledProperty<string> SubTitleProperty =
                AvaloniaProperty.Register<ExtendedWindow, string>(nameof(SubTitle));

        public static readonly StyledProperty<object?> TitleContentProperty
            = AvaloniaProperty.Register<ExtendedWindow, object?>(nameof (TitleContent));

        public object? TitleContent
        {
            get => GetValue(TitleContentProperty);
            set => SetValue(TitleContentProperty, value);
        }

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
        
        public Control? SideBar
        {
            get => GetValue(SideBarProperty);
            set => SetValue(SideBarProperty, value);
        }
        
        public StatusBar StatusBar
        {
            get => GetValue(StatusBarProperty);
            set => SetValue(StatusBarProperty, value);
        }
        
        public Control Overlay
        {
            get => GetValue(OverlayProperty);
            set => SetValue(OverlayProperty, value);
        }

        public string SubTitle
        {
            get => GetValue(SubTitleProperty);
            set => SetValue(SubTitleProperty, value);
        }
        
        public TabStrip TabStrip
        {
            get => GetValue(TabStripProperty);
            set => SetValue(TabStripProperty, value);
        }
        
        protected override Type StyleKeyOverride => typeof(ExtendedWindow);


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

            BackgroundProperty.Changed.AddClassHandler<ExtendedWindow>((window, e) =>
            {
                window.BindBackgroundBrush(e.NewValue as SolidColorBrush);
            });
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (OperatingSystem.IsWindows())
            {
                PlatformImpl?.GetType()
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(x => x.Name == "ShowWindow")?
                    .Invoke(this.PlatformImpl!, new object?[] { this.WindowState, false });
            }
        }

        private IDisposable? backgroundBrushBinding;
        private void UnbindBackgroundBrush()
        {
            backgroundBrushBinding?.Dispose();
            backgroundBrushBinding = null;
        }
        
        private void BindBackgroundBrush(SolidColorBrush? brush)
        {
            UnbindBackgroundBrush();

            if (brush != null)
            {
                backgroundBrushBinding = brush.GetObservable(SolidColorBrush.ColorProperty)
                    .Subscribe(_ => BackgroundBrushOnInvalidated(null, EventArgs.Empty));
            }
        }
        
        private void BackgroundBrushOnInvalidated(object? sender, EventArgs e)
        {
            if (Background is ISolidColorBrush brush)
                if (TryGetPlatformHandle() is { } handle)
                   Win32.SetTitleBarColor(handle.Handle, brush.Color);
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
            {
                if (TryGetPlatformHandle() is { } handle)
                    Win32.SetDarkMode(handle.Handle, SystemTheme.EffectiveThemeIsDark);
                PseudoClasses.Add(":windows");
                if (Environment.OSVersion.Version.Build >= 22000)
                    PseudoClasses.Add(":win11");
            }
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
                scaling = (primaryScreen?.Scaling ?? 1);
            }
            else
                scaling = SystemTheme.CustomScalingValue.Value;
            
            var impl = window.PlatformImpl;
            if (impl == null)
                return;
            var f = impl.GetType().GetField("_scaling", BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null)
            {
                var curVal = (double)f.GetValue(impl)!;
                if (Math.Abs(curVal - scaling) > 0.01)
                {
                    var oldWidth = window.Width * curVal;
                    var oldHeight = window.Height * curVal;
                    f.SetValue(impl, scaling);
                    Action<double>? scalingChanged = (Action<double>?)impl.GetType().GetProperty("ScalingChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(impl);
                    scalingChanged?.Invoke(scaling);
                    DispatcherTimer.RunOnce(() =>
                    {
                        window.Width = oldWidth / scaling;
                        window.Height = oldHeight / scaling;
                    }, TimeSpan.FromMilliseconds(1));
                }
            }
        }

        private void SystemThemeOnCustomScalingUpdated(double? obj)
        {
            UpdateScaling(this);
        }

        protected override void OnClosed(EventArgs e)
        {
            UnbindBackgroundBrush();
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
                if (oldChild is ToolBar tb)
                {
                    tb.PropertyChanged -= OnToolbarPropertyChanged;
                }
                LogicalChildren.Remove(oldChild);
                PseudoClasses.Remove(pseudoclass);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
                if (newChild is ToolBar tb)
                {
                    tb.PropertyChanged += OnToolbarPropertyChanged;
                    OnToolbarPropertyChanged(tb, null!);
                }
                else
                {
                    PseudoClasses.Add(pseudoclass);
                }
            }
        }

        private void OnToolbarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is ToolBar tb)
            {
                if (tb.IsEmpty)
                {
                    PseudoClasses.Remove(":has-toolbar");
                }
                else
                {
                    PseudoClasses.Add(":has-toolbar");
                }
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
