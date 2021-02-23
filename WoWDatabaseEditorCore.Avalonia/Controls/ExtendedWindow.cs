using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;

namespace WoWDatabaseEditorCore.Avalonia.Controls
{
    public class ExtendedWindow : Window, IStyleable
    {
        public static readonly StyledProperty<ExtendedTitleBar> ToolBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, ExtendedTitleBar>(nameof(ToolBar));

        public static readonly StyledProperty<IControl> SideBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, IControl>(nameof(SideBar));
        
        public static readonly StyledProperty<IControl> StatusBarProperty =
            AvaloniaProperty.Register<ExtendedWindow, IControl>(nameof(StatusBar));
        
        public static readonly StyledProperty<TabStrip> TabStripProperty =
            AvaloniaProperty.Register<ExtendedWindow, TabStrip>(nameof(TabStrip));
        
        public ExtendedTitleBar ToolBar
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
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            ExtendClientAreaChromeHints =
                ExtendClientAreaChromeHints.OSXThickTitleBar | ExtendClientAreaChromeHints.SystemChrome;

            if (SideBar == null && e.NameScope.Find("SidebarGrid") is Grid grid)
            {
                grid.ColumnDefinitions[0].MaxWidth = 0;
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
    }
}