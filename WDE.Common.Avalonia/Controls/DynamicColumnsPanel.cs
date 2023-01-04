using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace WDE.Common.Avalonia.Controls
{
    public class DynamicColumnsPanel: Panel, INavigableContainer
    {
        public static readonly StyledProperty<double> SpacingProperty =
            AvaloniaProperty.Register<DynamicColumnsPanel, double>(nameof(Spacing));

        public static readonly StyledProperty<double> HorizontalSpacingProperty =
            AvaloniaProperty.Register<DynamicColumnsPanel, double>(nameof(HorizontalSpacing));

        public static readonly StyledProperty<double> ColumnWidthProperty =
            AvaloniaProperty.Register<DynamicColumnsPanel, double>(nameof(ColumnWidth), defaultValue: 200);

        public static readonly StyledProperty<int> MaximumColumnsCountProperty =
            AvaloniaProperty.Register<DynamicColumnsPanel, int>(nameof(MaximumColumnsCount), defaultValue: int.MaxValue);

        public double ColumnWidth
        {
            get => GetValue(ColumnWidthProperty);
            set => SetValue(ColumnWidthProperty, value);
        }

        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }
        
        public double HorizontalSpacing
        {
            get => GetValue(HorizontalSpacingProperty);
            set => SetValue(HorizontalSpacingProperty, value);
        }
        
        public int MaximumColumnsCount
        {
            get => GetValue(MaximumColumnsCountProperty);
            set => SetValue(MaximumColumnsCountProperty, value);
        }
        
        static DynamicColumnsPanel()
        {
            AffectsMeasure<DynamicColumnsPanel>(SpacingProperty);
            AffectsMeasure<DynamicColumnsPanel>(HorizontalSpacingProperty);
            AffectsMeasure<DynamicColumnsPanel>(ColumnWidthProperty);
            AffectsMeasure<DynamicColumnsPanel>(MaximumColumnsCountProperty);
            
            AffectsArrange<DynamicColumnsPanel>(MaximumColumnsCountProperty);
            
            AffectsParentMeasure<DynamicColumnsPanel>(MaximumColumnsCountProperty);
            AffectsParentArrange<DynamicColumnsPanel>(MaximumColumnsCountProperty);
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
        /// <returns>The control.</returns>
        IInputElement INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var result = GetControlInDirection(direction, from as Control);

            if (result == null && wrap)
            {
                switch (direction)
                {
                    case NavigationDirection.Up:
                    case NavigationDirection.Previous:
                    case NavigationDirection.PageUp:
                        result = GetControlInDirection(NavigationDirection.Last, null);
                        break;
                    case NavigationDirection.Down:
                    case NavigationDirection.Next:
                    case NavigationDirection.PageDown:
                        result = GetControlInDirection(NavigationDirection.First, null);
                        break;
                }
            }

            return result!;
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <returns>The control.</returns>
        protected virtual IInputElement? GetControlInDirection(NavigationDirection direction, Control? from)
        {
            int index = from != null ? Children.IndexOf(from) : -1;

            switch (direction)
            {
                case NavigationDirection.First:
                    index = 0;
                    break;
                case NavigationDirection.Last:
                    index = Children.Count - 1;
                    break;
                case NavigationDirection.Next:
                    if (index != -1) ++index;
                    break;
                case NavigationDirection.Previous:
                    if (index != -1) --index;
                    break;
                case NavigationDirection.Left:
                    if (index != -1) index = -1;
                    break;
                case NavigationDirection.Right:
                    if (index != -1) index = -1;
                    break;
                case NavigationDirection.Up:
                    if (index != -1) index = index - 1;
                    break;
                case NavigationDirection.Down:
                    if (index != -1) index = index + 1;
                    break;
                default:
                    index = -1;
                    break;
            }

            if (index >= 0 && index < Children.Count)
            {
                return Children[index];
            }
            else
            {
                return null;
            }
        }

        private int GetColumnsCount(Size size)
        {
            if (double.IsInfinity(size.Width))
                return Math.Min(MaximumColumnsCount, Children.Count);
            return Math.Min((int)Math.Max(Math.Floor(size.Width / ColumnWidth), 1), MaximumColumnsCount);
        }

        private double GetActualColumnWidth(Size size)
        {
            if (double.IsInfinity(size.Width))
                return ColumnWidth;
            var count = GetColumnsCount(size);
            return (size.Width - Math.Max(count - 1, 0) * HorizontalSpacing) / count;
        }

        public IEnumerable<Control> VisibleChildren()
        {
            for (int i = 0, count = Children.Count; i < count; ++i)
            {
                // Get next child.
                var child = Children[i];

                if (child == null)
                {
                    continue;
                }

                bool isVisible = child.IsVisible;

                if (isVisible)
                    yield return child;
            }
        }
        
        /// <summary>
        /// General StackPanel layout behavior is to grow unbounded in the "stacking" direction (Size To Content).
        /// Children in this dimension are encouraged to be as large as they like.  In the other dimension,
        /// StackPanel will assume the maximum size of its children.
        /// </summary>
        /// <param name="availableSize">Constraint</param>
        /// <returns>Desired size</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            Size stackDesiredSize = new Size();
            var children = Children;
            Size layoutSlotSize = availableSize;
            double spacing = Spacing;
            bool hasVisibleChild = false;

            //
            // Initialize child sizing and iterator data
            // Allow children as much size as they want along the stack.
            //
            layoutSlotSize = layoutSlotSize.WithHeight(Double.PositiveInfinity);

            //
            //  Iterate through children.
            //  While we still supported virtualization, this was hidden in a child iterator (see source history).
            //
            foreach (var child in VisibleChildren())
            {
                hasVisibleChild = true;

                // Measure the child.
                child.Measure(layoutSlotSize);
                Size childDesiredSize = child.DesiredSize;

                // Accumulate child size.
                stackDesiredSize = stackDesiredSize.WithWidth(Math.Max(stackDesiredSize.Width, childDesiredSize.Width));
                stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height + spacing + childDesiredSize.Height);
            }

            stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height - (hasVisibleChild ? spacing : 0));

            // columns
            int columns = GetColumnsCount(availableSize);
            double columnWidth = GetActualColumnWidth(availableSize);
            double averageHeight = stackDesiredSize.Height / columns;

            double y = 0;
            double maxHeight = 0;
            // second pass, actually measure max height
            foreach (var child in VisibleChildren())
            {
                y += child.DesiredSize.Height + spacing;
                if (y > averageHeight)
                {
                    y -= spacing;
                    maxHeight = Math.Max(maxHeight, y);
                    y = 0;
                } else
                    maxHeight = Math.Max(maxHeight, y);
            }
            return new Size(stackDesiredSize.Width, maxHeight);
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="finalSize">Arrange size</param>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = Children;
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0;
            var spacing = Spacing;

            int columns = GetColumnsCount(finalSize);
            double columnWidth = GetActualColumnWidth(finalSize.WithWidth(finalSize.Width));

            double x = 0;
            double y = 0;
            int columnIndex = 1;
            //
            // Arrange and Position Children.
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                { continue; }

                rcChild = rcChild.WithX(x);
                rcChild = rcChild.WithY(y);
                previousChildSize = child.DesiredSize.Height;
                rcChild = rcChild.WithHeight(previousChildSize);
                rcChild = rcChild.WithWidth(columnWidth);
                previousChildSize += spacing;

                y += previousChildSize;
                
                if (y >= DesiredSize.Height)
                {
                    x += columnWidth + (columnIndex < columns ? HorizontalSpacing : 0);
                    y = 0;
                    columnIndex++;
                }

                ArrangeChild(child, rcChild, finalSize);
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
}