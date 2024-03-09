using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using WDE.Common;

namespace AvaloniaStyles.Controls
{
    public class ToolbarPanel : Panel
    {
        public static readonly StyledProperty<Panel?> OutOfBoundsPanelProperty = AvaloniaProperty.Register<ToolbarPanel, Panel?>(nameof(OutOfBoundsPanel));
        public static readonly StyledProperty<bool> IsOverflowProperty = AvaloniaProperty.Register<ToolbarPanel, bool>(nameof(IsOverflow));
        public static readonly StyledProperty<bool> WrapOnOverflowProperty = AvaloniaProperty.Register<ToolbarPanel, bool>(nameof(WrapOnOverflow));
        public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<ToolbarPanel, double>(nameof(Spacing), 4);

        private List<Control> _overflowControls = new List<Control>();
        
        public Panel? OutOfBoundsPanel
        {
            get => (Panel?)GetValue(OutOfBoundsPanelProperty);
            set => SetValue(OutOfBoundsPanelProperty, value);
        }
        
        public bool IsOverflow
        {
            get => (bool)GetValue(IsOverflowProperty);
            set => SetValue(IsOverflowProperty, value);
        }
        
        public bool WrapOnOverflow
        {
            get => (bool)GetValue(WrapOnOverflowProperty);
            set => SetValue(WrapOnOverflowProperty, value);
        }

        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        delegate void InvalidateStylesType(StyledElement element, bool recurse);
        private static InvalidateStylesType? invalidateStyle;

        static ToolbarPanel()
        {
            var invalidateStyleMethod = typeof(StyledElement).GetMethod("InvalidateStyles", BindingFlags.Instance | BindingFlags.NonPublic);
            if (invalidateStyleMethod != null)
                invalidateStyle = (InvalidateStylesType)Delegate.CreateDelegate(typeof(InvalidateStylesType), invalidateStyleMethod);
            else
                LOG.LogCritical("Failed to find InvalidateStyles method (probably due to Avalonia version update)!!!");
    
            OutOfBoundsPanelProperty.Changed.AddClassHandler<ToolbarPanel>((panel, e) => panel.OnChangedPanel(e));
            AffectsMeasure<ToolbarPanel>(SpacingProperty);
            AffectsArrange<ToolbarPanel>(SpacingProperty);
        }

        private void OnChangedPanel(AvaloniaPropertyChangedEventArgs changed)
        {
            var newPanel = changed.NewValue as Panel;
            if (newPanel == null)
                return;
            newPanel.AttachedToVisualTree += NewPanelOnAttachedToVisualTree;
        }

        private void NewPanelOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            foreach (var child in _overflowControls)
            {
                OutOfBoundsPanel!.Children.Insert(0, child);
                // @todo ava11: is that necessary?
                child.ApplyStyling();
            }
            _overflowControls.Clear();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (OutOfBoundsPanel is { } panel)
            {
                for (var index = panel.Children.Count - 1; index >= 0; index--)
                {
                    var child = panel.Children[0];
                    panel.Children.RemoveAt(0);
                    Children.Add(child);
                }
            }

            if (_overflowControls.Count > 0)
            {
                foreach (var control in _overflowControls)
                    Children.Add(control);
                _overflowControls.Clear();
            }
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            if (WrapOnOverflow)
                return MeasureWithWrapOverride(availableSize);
            var wrap = WrapOnOverflow;
            var spacing = Spacing;
            double desiredWidth = 0;
            double desiredHeight = 0;
            double maxDesiredWidth = 0;
            double maxDesiredHeight = 0;
            int rows = 1;
            var children = Children;
            bool any = false;

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null)
                    continue;

                if (!child.IsVisible)
                {
                    child.Measure(availableSize.WithWidth(double.PositiveInfinity));
                    continue;
                }

                if (child is ToolbarSpacer)
                    continue;
                
                any = true;
                child.Measure(availableSize.WithWidth(double.PositiveInfinity));
                desiredWidth += child.DesiredSize.Width + spacing;
                desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
                if (wrap && desiredWidth > availableSize.Width)
                {
                    maxDesiredWidth = Math.Max(maxDesiredWidth, desiredWidth - spacing);
                    maxDesiredHeight = Math.Max(maxDesiredHeight, desiredHeight);
                    desiredWidth = 0;
                    desiredHeight = 0;
                    rows++;
                }
            }

            if (desiredWidth > 0)
            {
                maxDesiredWidth = Math.Max(maxDesiredWidth, desiredWidth);
                maxDesiredHeight = Math.Max(maxDesiredHeight, desiredHeight);
            }

            foreach (var child in _overflowControls)
            {
                maxDesiredWidth += child.DesiredSize.Width + spacing;
            }
            
            if (OutOfBoundsPanel != null)
            {
                OutOfBoundsPanel.Measure(availableSize.WithWidth(double.PositiveInfinity));
                maxDesiredWidth += OutOfBoundsPanel.DesiredSize.Width + spacing;
            }

            return new Size(any ? maxDesiredWidth : 0, maxDesiredHeight * rows);
        }
        
        private Size MeasureWithWrapOverride(Size availableSize)
        {
            var children = Children;
            double x = 0;
            double y = 0;
            double maxWidth = availableSize.Width;
            double maxDesiredHeight = 0;
            double maxEffectiveWidth = maxWidth;
            int rows = 1;
            bool anyInRow = false;

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;
                
                child.Measure(availableSize.WithWidth(double.PositiveInfinity));
                
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
            }
            
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;

                var desiredSize = child.DesiredSize;
                if (anyInRow && x + desiredSize.Width > maxWidth)
                {
                    maxEffectiveWidth = Math.Max(maxEffectiveWidth, x);
                    anyInRow = false;
                    x = 0;
                    y += maxDesiredHeight;
                    rows++;
                }
                else
                    anyInRow = true;
                
                var finalChildWidth = Math.Max(1, Math.Min(desiredSize.Width, maxWidth - x));
                x += finalChildWidth;
            }
            
            return new Size(rows == 1 ? x : maxEffectiveWidth, rows * maxDesiredHeight);
        }
        
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (WrapOnOverflow)
                return ArrangeWithWrapOverride(finalSize);
            
            var children = Children;
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0;
            var spacing = Spacing;

            int spacerCount = 0;
            double totalDesiredWidth = 0;
            bool any = false;
            
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;
                if (child is ToolbarSpacer)
                    spacerCount++;
                else
                {
                    any = true;
                    totalDesiredWidth += child.DesiredSize.Width + spacing;
                }
            }
            if (any)
                totalDesiredWidth -= spacing;

            double leftSpace = finalSize.Width - totalDesiredWidth;
            double spacerWidth = spacerCount == 0 ? 0 : (leftSpace > 0 ? leftSpace / spacerCount : 0.0);
            
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;

                child.ApplyTemplate();
                
                rcChild = rcChild.WithX(rcChild.X + previousChildSize);
                rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
                // we want to stretch only content presenters, not buttons
                // if it doesn't work for future toolbars, it can be changed 
                if (child.HorizontalAlignment == HorizontalAlignment.Stretch &&
                    (child is ContentPresenter { Child: not Button } || (child is ContentControl)))
                {
                    previousChildSize = child.DesiredSize.Width + Math.Max(0, leftSpace);
                    rcChild = rcChild.WithWidth(previousChildSize);
                    previousChildSize += spacing;
                }
                else if (child is ToolbarSpacer)
                {
                    previousChildSize = spacerWidth;
                    rcChild = rcChild.WithWidth(previousChildSize);
                }
                else
                {
                    previousChildSize = child.DesiredSize.Width;
                    rcChild = rcChild.WithWidth(previousChildSize);
                    previousChildSize += spacing;
                }
                if (rcChild.Right > finalSize.Width)
                    rcChild = rcChild.WithWidth(Math.Max(0, finalSize.Width - rcChild.X));

                if (rcChild.Right > finalSize.Width)
                {
                    if (OutOfBoundsPanel is { } panel)
                    {
                        for (var j = i; j < count; ++j)
                        {
                            var c = children[^1];
                            Children.RemoveAt(Children.Count - 1);
                            if (panel.IsAttachedToVisualTree())
                            {
                                panel.Children.Insert(0, c);
                                ((ISetInheritanceParent)c).SetParent(this);
                            }
                            else
                                _overflowControls.Add(c);
                        }                        
                        SetCurrentValue(IsOverflowProperty, true);
                        return finalSize;
                    }
                }
                ArrangeChild(child, rcChild, finalSize);
            }

            if (OutOfBoundsPanel != null)
            {
                if (leftSpace > 0)
                {
                    Control? child = null;
                    if (_overflowControls.Count > 0)
                    {
                        child = _overflowControls[^1];
                    }
                    else if (OutOfBoundsPanel.Children.Count > 0)
                    {
                        child = OutOfBoundsPanel.Children[0];
                    }

                    if (child != null)
                    {
                        if (leftSpace > child.Bounds.Width + spacing)
                        {
                            if (_overflowControls.Count > 0)
                                _overflowControls.RemoveAt(_overflowControls.Count - 1);
                            else
                                OutOfBoundsPanel.Children.RemoveAt(0);
                            Children.Add(child);
                            // it really sucks I have to call InvalidateStyles via reflection, but without this
                            // buttons style bug in Solution Explorer in the following case
                            // shrink the toolbar to overflow buttons
                            // open the flyout
                            // expand the toolbar to show all buttons - now the buttons are not styled
                            invalidateStyle?.Invoke(child, true);
                            child.ApplyStyling();
                        }
                    }
                }
                SetCurrentValue(IsOverflowProperty, OutOfBoundsPanel.Children.Count > 0 || _overflowControls.Count > 0);
            }

            return finalSize;
        }

        private Size ArrangeWithWrapOverride(Size finalSize)
        {
            var children = Children;
            double x = 0;
            double y = 0;
            double maxWidth = finalSize.Width;
            double maxDesiredHeight = 0;
            double maxEffectiveWidth = maxWidth;
            int rows = 1;
            bool anyInRow = false;

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;
                
                maxDesiredHeight = Math.Max(maxDesiredHeight, child.DesiredSize.Height);
            }
            
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                    continue;

                var desiredSize = child.DesiredSize;
                if (anyInRow && x + desiredSize.Width > maxWidth)
                {
                    maxEffectiveWidth = Math.Max(maxEffectiveWidth, x);
                    anyInRow = false;
                    x = 0;
                    y += maxDesiredHeight;
                    rows++;
                }
                else
                    anyInRow = true;
                
                var finalChildWidth = Math.Max(1, Math.Min(desiredSize.Width, maxWidth - x));
                child.Arrange(new Rect(x, y, finalChildWidth, maxDesiredHeight));
                x += finalChildWidth;
            }
            
            return new Size(rows == 1 ? x : maxEffectiveWidth, rows * maxDesiredHeight);
        }

        internal virtual void ArrangeChild(
            Control child,
            Rect rect,
            Size panelSize)
        {
            child.Arrange(rect);
        }
    }

    public class ToolbarSpacer : Control
    {
    }
}
