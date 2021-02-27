using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace AvaloniaStyles.Controls
{
    public class ExtendedWindow : Window, IStyleable
    {
        public static readonly StyledProperty<IImage> ManagedIconProperty =
            AvaloniaProperty.Register<ExtendedWindow, IImage>(nameof(ManagedIcon));
        
        public static readonly StyledProperty<bool> UseSystemChromeProperty =
            AvaloniaProperty.Register<ExtendedWindow, bool>(nameof(UseSystemChrome));
        
        public static readonly StyledProperty<ToolBar> ToolBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, ToolBar>(nameof(ToolBar));

        public static readonly StyledProperty<IControl> SideBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, IControl>(nameof(SideBar));
        
        public static readonly StyledProperty<IControl> StatusBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, IControl>(nameof(StatusBar));
        
        public static readonly StyledProperty<TabStrip> TabStripProperty =
            AvaloniaProperty.Register<ExtendedWindow, TabStrip>(nameof(TabStrip));
        
        public IImage ManagedIcon
        {
            get => GetValue(ManagedIconProperty);
            set => SetValue(ManagedIconProperty, value);
        }
        
        public bool UseSystemChrome
        {
            get => GetValue(UseSystemChromeProperty);
            set => SetValue(UseSystemChromeProperty, value);
        }
        
        public ToolBar ToolBar
        {
            get => GetValue(ToolBarProperty);
            set => SetValue(ToolBarProperty, value);
        }
        
        public IControl SideBar
        {
            get => GetValue(SideBarProperty);
            set => SetValue(SideBarProperty, value);
        }
        
        public IControl StatusBar
        {
            get => GetValue(StatusBarProperty);
            set => SetValue(StatusBarProperty, value);
        }
        
        public TabStrip TabStrip
        {
            get => GetValue(TabStripProperty);
            set => SetValue(TabStripProperty, value);
        }
        
        Type IStyleable.StyleKey => typeof(ExtendedWindow);
        
        static ExtendedWindow()
        {
            ToolBarProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-toolbar"));
            SideBarProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-sidebar"));
            StatusBarProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-statusbar"));
            TabStripProperty.Changed.AddClassHandler<ExtendedWindow>((x, e) => x.ContentChanged(e, ":has-tabstrip"));

            WindowStateProperty.Changed.AddClassHandler<ExtendedWindow>((window, state) =>
            {
                window.PseudoClasses.Set(":maximized", (WindowState)state.NewValue == WindowState.Maximized);
                window.PseudoClasses.Set(":fullscreen", (WindowState)state.NewValue == WindowState.FullScreen);
            });

            UseSystemChromeProperty.Changed.AddClassHandler<ExtendedWindow>((window, state) =>
            {
                if (state.NewValue is bool b)
                    window.UpdateChromeHints(b);
            });
        }

        private void UpdateChromeHints(bool useChrome)
        {
            if (useChrome)
            {
                ExtendClientAreaChromeHints |= ExtendClientAreaChromeHints.SystemChrome;
                ExtendClientAreaChromeHints &= ~ExtendClientAreaChromeHints.NoChrome;
            }
            else
            {
                ExtendClientAreaChromeHints &= ~ExtendClientAreaChromeHints.SystemChrome;
                ExtendClientAreaChromeHints |= ExtendClientAreaChromeHints.NoChrome;
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
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            ExtendClientAreaChromeHints |= ExtendClientAreaChromeHints.OSXThickTitleBar;
            UpdateChromeHints(UseSystemChrome);

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

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            PseudoClasses.Add(":focused");
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            PseudoClasses.Remove(":focused");
        }

        public void MaximizeNormalize()
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }
    }
}