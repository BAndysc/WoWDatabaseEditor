using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaStyles.Controls;
using WDE.Common.Avalonia.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using static System.Math;

namespace WDE.SmartScriptEditor.Avalonia.Editor
{
    // this is one big hack and should be refactored completely, grounds up
    public class FastGroupingWrapPanel : RenderedPanel, INavigableContainer
    {
        protected static ISolidColorBrush HeaderForeground = new SolidColorBrush(new Color(0xFF, 0x22, 0x6E, 0x8B));

        private double itemWidth;
        public static readonly DirectProperty<FastGroupingWrapPanel, double> ItemWidthProperty = AvaloniaProperty.RegisterDirect<FastGroupingWrapPanel, double>("ItemWidth", o => o.ItemWidth, (o, v) => o.ItemWidth = v);
        private double itemHeight;
        public static readonly DirectProperty<FastGroupingWrapPanel, double> ItemHeightProperty = AvaloniaProperty.RegisterDirect<FastGroupingWrapPanel, double>("ItemHeight", o => o.ItemHeight, (o, v) => o.ItemHeight = v);

        public static readonly AttachedProperty<string> GroupProperty =
            AvaloniaProperty.RegisterAttached<FastGroupingWrapPanel, Control, string>("Group");
        
        public static readonly AttachedProperty<bool> ActiveProperty =
            AvaloniaProperty.RegisterAttached<FastGroupingWrapPanel, Control, bool>("Active");
        
        private ScrollViewer? ScrollViewer => this.FindAncestorOfType<ScrollViewer>();
        private IDisposable? scrollDisposable;

        static FastGroupingWrapPanel()
        {
            ResourcesUtils.Get("GroupingHeaderColor", HeaderForeground, out HeaderForeground);
            AffectsParentMeasure<FastGroupingWrapPanel>(GroupProperty);
            AffectsParentMeasure<FastGroupingWrapPanel>(IsVisibleProperty);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            var scrollViewer = ScrollViewer;
            scrollDisposable = scrollViewer!.ToObservable(x => x.Offset)
                .SubscribeAction(_ =>
                {
                    InvalidateMeasure();
                    InvalidateArrange();
                });
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            scrollDisposable?.Dispose();
            scrollDisposable = null;
        }

        public static string GetGroup(Control control)
        {
            return (string?)control.GetValue(GroupProperty) ?? "(null)";
        }
        
        public static void SetGroup(Control control, string value)
        {
            control.SetValue(GroupProperty, value);
        }
        
        public static bool GetActive(Control control)
        {
            return (bool?)control.GetValue(ActiveProperty) ?? false;
        }
        
        public static void SetActive(Control control, bool value)
        {
            control.SetValue(ActiveProperty, value);
        }
        
        public double ItemWidth
        {
            get => itemWidth;
            set => SetAndRaise(ItemWidthProperty, ref itemWidth, value);
        }

        public double ItemHeight
        {
            get => itemHeight;
            set => SetAndRaise(ItemHeightProperty, ref itemHeight, value);
        }
        
        public override void Render(DrawingContext context)
        {
            var brush = HeaderForeground;
            var pen = new Pen(brush, 1);
            foreach (var group in groupStartHeight)
            {
                if (group.Value.height < float.Epsilon)
                    continue;
                var lineY = group.Value.Item1 + 15;
                var ft = new FormattedText(group.Key, CultureInfo.CurrentCulture,  FlowDirection.LeftToRight, 
                    new Typeface(TextElement.GetFontFamily(this), FontStyle.Normal, FontWeight.Bold), 12, brush);
                context.DrawLine(pen, new Point(0, lineY), new Point(15, lineY));
                context.DrawText(ft, new Point(20, group.Value.Item1 + 16 - ft.Height / 2));
                context.DrawLine(pen, new Point(20 + ft.Width + 5, lineY), new Point(this.Bounds.Width, lineY));
            }
        }

        private void BuildGrid()
        {
            if (!gridDirty)
                return;

            var itemsPerRow = (int)Max(1, Floor(this.Bounds.Width / itemWidth));
            if (grid.GetLength(0) < itemsPerRow || grid.GetLength(1) < VisualChildren.Count)
                grid = new Control[(int)itemsPerRow, VisualChildren.Count];
            else
            {
                for (int i = 0; i < grid.GetLength(0); ++i)
                {
                    for (int j = 0; j < grid.GetLength(1); ++j)
                        grid[i, j] = null;
                }
            }
            
            int y = 0;
            foreach (var group in elementsInGroup)
            {
                int x = 0;
                foreach (var item in group.Value)
                {
                    grid[x, y] = item;
                    x++;
                    if (x == itemsPerRow)
                    {
                        x = 0;
                        y++;
                    }
                }

                if (x != 0)
                    y++;
            }
            
            gridDirty = false;
        }
        
        Dictionary<string, List<Control>> elementsInGroup = new();
        Dictionary<string, (double y, double x, double height, int count)> groupStartHeight = new();
        private Control?[,] grid = new Control[0, 0];
        private bool gridDirty = true;
        protected override Size MeasureOverride(Size availableSize)
        {
            double height = 0;
            gridDirty = true;

            EnsureRenderer();

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;
            
            elementsInGroup.Clear();
            groupStartHeight.Clear();
            elementsInGroup["Favourites"] = new List<Control>() { };
            Control? renderer = null;
            for (var i = 0; i < visualCount; i++)
            {
                Visual visual = visualChildren[i];

                if (IsRenderer(visual))
                {
                    renderer = (Control)visual;
                    continue;
                }

                if (visual is Control control && GetActive(control))
                {
                    var group = GetGroup(control) ?? "(default)";
                    if (!elementsInGroup.ContainsKey(group))
                        elementsInGroup[group] = new List<Control>() { control };
                    else
                        elementsInGroup[group].Add(control);
                }
            }

            var itemsPerRow = Max(1, Floor(availableSize.Width / itemWidth));
            foreach (var group in elementsInGroup)
            {
                if (group.Value.Count > 0)
                {
                    height += 32; // header
                    groupStartHeight[group.Key] = (height - 32, 0, height, 0);
                    height += itemHeight * Ceiling(group.Value.Count / itemsPerRow);                    
                }
                else
                    groupStartHeight[group.Key] = (0, 0, 0, 0);
            }
            
            var desiredSize = new Size(Min(availableSize.Width, visualCount * itemWidth), height);

            renderer?.Measure(desiredSize);

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var scrollViewer = ScrollViewer!;
            // extended viewport rect to display more items than the viewport for proper on arrows (UP/DOWN) down scrolling
            var viewPortRect = new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height).Inflate(60);
            var arrangeRect = new Rect(finalSize);

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            var itemsPerRow = Max(1, (int)Floor(finalSize.Width / itemWidth));
            
            MeasureOverride(finalSize);
            
            if (visualCount == 0)
                return default;

            for (var i = 0; i < visualCount; i++)
            {
                Control? control = visualChildren[i] as Control;
                if (control == null)
                    continue;

                if (IsRenderer(control))
                {
                    control.Arrange(new Rect(0, 0, scrollViewer.Extent.Width, scrollViewer.Extent.Width));
                    continue;
                }

                if (GetActive(control))
                {
                    var group = GetGroup(control);
                    if (groupStartHeight.TryGetValue(group, out var startPos))
                    {
                        double x = startPos.Item2;
                        double y = startPos.Item3;
                        var rect = new Rect(x, y, itemWidth, itemHeight);
                        if (rect.Intersects(viewPortRect))
                        {
                            control.IsVisible = true;
                            control.Arrange(rect);
                        }

                        if (startPos.Item4 + 1 == itemsPerRow)
                            groupStartHeight[group] = (startPos.Item1, 0, y + itemHeight, 0);
                        else
                            groupStartHeight[group] = (startPos.Item1, x + itemWidth, y, startPos.Item4 + 1);
                    }
                }
                else
                    control.IsVisible = false;
            }
            InvalidateVisual();
            return finalSize;
        }

        private (int, int)? Find(IInputElement? element)
        {
            BuildGrid();
            for (int i = 0; i < grid.GetLength(0); ++i)
            {
                for (int j = 0; j < grid.GetLength(1); ++j)
                {
                    if (grid[i, j] == element)
                    {
                        return (i, j);
                    }
                }
            }

            return null;
        }
        
        public IInputElement? GetControl(NavigationDirection direction, IInputElement? @from, bool wrap)
        {
            Control? control = (Control?)@from;
            var index = Find(from);
            if (index == null)
                return from;
            
            switch (direction)
            {
                case NavigationDirection.Next:
                case NavigationDirection.PageDown:
                case NavigationDirection.PageUp:
                case NavigationDirection.Last:
                case NavigationDirection.First:
                case NavigationDirection.Previous:
                    break;
                case NavigationDirection.Left:
                    return GetElementInDir(index.Value, (-1, 0))!;
                case NavigationDirection.Right:
                    return GetElementInDir(index.Value, (1, 0))!;
                case NavigationDirection.Up:
                    return GetElementInDir(index.Value, (0, -1))!;
                case NavigationDirection.Down:
                    return GetElementInDir(index.Value, (0, 1))!;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            return @from;
        }

        private IInputElement? GetElementInDir((int x, int y) start, (int x, int y) dir)
        {
            BuildGrid();
            (int x, int y) cur = (start.x + dir.x, start.y + dir.y);
            while (cur.x >= 0 && cur.x < grid.GetLength(0) && 
                   cur.y >= 0 && cur.y < grid.GetLength(1) &&
                   grid[cur.x, cur.y] == null)
                cur = (cur.x + dir.x, cur.y + dir.y);

            if (cur.x >= 0 && cur.x < grid.GetLength(0) &&
                cur.y >= 0 && cur.y < grid.GetLength(1))
                return grid[cur.x, cur.y];
            return null;
        }
    }
}