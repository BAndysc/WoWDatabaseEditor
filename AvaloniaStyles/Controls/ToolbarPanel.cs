using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls
{
    public class ToolbarPanel : Panel
    {
        public static readonly StyledProperty<Panel?> OutOfBoundsPanelProperty = AvaloniaProperty.Register<ToolbarPanel, Panel?>(nameof(OutOfBoundsPanel));
        public static readonly StyledProperty<bool> IsOverflowProperty = AvaloniaProperty.Register<ToolbarPanel, bool>("IsOverflow");
        public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<ToolbarPanel, double>("Spacing", 4);

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
        
        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        static ToolbarPanel()
        {
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
                OutOfBoundsPanel!.Children.Add(child);
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
            var spacing = Spacing;
            double desiredWidth = 0;
            double desiredHeight = 0;
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
            }

            foreach (var child in _overflowControls)
            {
                desiredWidth += child.DesiredSize.Width + spacing;
            }
            
            if (OutOfBoundsPanel != null)
            {
                OutOfBoundsPanel.Measure(availableSize.WithWidth(double.PositiveInfinity));
                desiredWidth += OutOfBoundsPanel.DesiredSize.Width + spacing;
            }

            return new Size(any ? desiredWidth - spacing : 0, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
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

                rcChild = rcChild.WithX(rcChild.X + previousChildSize);
                rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
                // we want to stretch only content presenters, not buttons
                // if it doesn't work for future toolbars, it can be changed 
                if (child.HorizontalAlignment == HorizontalAlignment.Stretch && child is ContentPresenter)
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

                if (rcChild.Right > finalSize.Width && OutOfBoundsPanel is { } panel)
                {
                    for (var j = i; j < count; ++j)
                    {
                        var c = children[^1];
                        Children.RemoveAt(Children.Count - 1);
                        if (panel!.GetVisualRoot() != null)
                            panel.Children.Insert(0, c);
                        else
                            _overflowControls.Insert(0, c);
                    }

                    IsOverflow = true;
                    return finalSize;
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
                        child = _overflowControls[0];
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
                                _overflowControls.RemoveAt(0);
                            else
                                OutOfBoundsPanel.Children.RemoveAt(0);
                            Children.Add(child);
                        }
                    }
                }
                IsOverflow = OutOfBoundsPanel.Children.Count > 0 || _overflowControls.Count > 0;
            }

            return finalSize;
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