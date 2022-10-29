using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using WDE.Common.Avalonia.Utils;
using WDE.SmartScriptEditor.Avalonia.Editor.Views;
using static System.Math;

namespace WDE.SmartScriptEditor.Avalonia.Editor
{
    public class FastGroupingWrapPanel : Panel, INavigableContainer
    {
        protected static ISolidColorBrush HeaderForeground = new SolidColorBrush(new Color(0xFF, 0x22, 0x6E, 0x8B));

        private double itemWidth;
        public static readonly DirectProperty<FastGroupingWrapPanel, double> ItemWidthProperty = AvaloniaProperty.RegisterDirect<FastGroupingWrapPanel, double>("ItemWidth", o => o.ItemWidth, (o, v) => o.ItemWidth = v);
        private double itemHeight;
        public static readonly DirectProperty<FastGroupingWrapPanel, double> ItemHeightProperty = AvaloniaProperty.RegisterDirect<FastGroupingWrapPanel, double>("ItemHeight", o => o.ItemHeight, (o, v) => o.ItemHeight = v);

        public static readonly AttachedProperty<string> GroupProperty =
            AvaloniaProperty.RegisterAttached<FastGroupingWrapPanel, IControl, string>("Group");

        static FastGroupingWrapPanel()
        {
            ResourcesUtils.Get("GroupingHeaderColor", HeaderForeground, out HeaderForeground);
            AffectsParentMeasure<FastGroupingWrapPanel>(GroupProperty);
            AffectsParentMeasure<FastGroupingWrapPanel>(IsVisibleProperty);
        }
        
        public static string GetGroup(IControl control)
        {
            return control.GetValue(GroupProperty);
        }
        
        public static void SetGroup(IControl control, string value)
        {
            control.SetValue(GroupProperty, value);
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
            base.Render(context);
            var brush = HeaderForeground;
            var pen = new Pen(brush, 1);
            foreach (var group in groupStartHeight)
            {
                if (group.Value.height < float.Epsilon)
                    continue;
                var lineY = group.Value.Item1 + 15;
                var ft = new FormattedText();
                ft.FontSize = 12;
                ft.Text = group.Key;
                ft.Typeface = new Typeface(TextBlock.GetFontFamily(this), FontStyle.Normal, FontWeight.Bold);
                context.DrawLine(pen, new Point(0, lineY), new Point(15, lineY));
                context.DrawText(brush, new Point(20, group.Value.Item1 + 16 - ft.Bounds.Height / 2), ft);
                context.DrawLine(pen, new Point(20 + ft.Bounds.Width + 5, lineY), new Point(this.Bounds.Width, lineY));
            }
        }

        private void BuildGrid()
        {
            if (!gridDirty)
                return;

            var itemsPerRow = (int)Max(1, Floor(this.Bounds.Width / itemWidth));
            if (grid.GetLength(0) < itemsPerRow || grid.GetLength(1) < VisualChildren.Count)
                grid = new IControl[(int)itemsPerRow, VisualChildren.Count];
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
        
        Dictionary<string, List<IControl>> elementsInGroup = new();
        Dictionary<string, (double y, double x, double height, int count)> groupStartHeight = new();
        private IControl?[,] grid = new IControl[0, 0];
        private bool gridDirty = true;
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;
            gridDirty = true;

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            elementsInGroup.Clear();
            groupStartHeight.Clear();
            elementsInGroup["Favourites"] = new List<IControl>() { };
            for (var i = 0; i < visualCount; i++)
            {
                IVisual visual = visualChildren[i];

                if (visual is ILayoutable layoutable && visual is IControl control 
                                                     && layoutable.IsVisible)
                {
                    var group = GetGroup(control);
                    if (!elementsInGroup.ContainsKey(group))
                        elementsInGroup[group] = new List<IControl>() { control };
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
            
            return new Size(Min(availableSize.Width, visualCount * itemWidth), height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var arrangeRect = new Rect(finalSize);

            var visualChildren = VisualChildren;
            var visualCount = visualChildren.Count;

            var itemsPerRow = Max(1, (int)Floor(finalSize.Width / itemWidth));
            
            MeasureOverride(finalSize);
            
            for (var i = 0; i < visualCount; i++)
            {
                IVisual visual = visualChildren[i];

                if (visual is ILayoutable layoutable && visual is IControl control && layoutable.IsVisible)
                {
                    var group = GetGroup(control);
                    if (groupStartHeight.TryGetValue(group, out var startPos))
                    {
                        double x = startPos.Item2;
                        double y = startPos.Item3;
                        layoutable.Arrange(new Rect(x, y, itemWidth, itemHeight));
                        if (startPos.Item4 + 1 == itemsPerRow)
                            groupStartHeight[group] = (startPos.Item1, 0, y + itemHeight, 0);
                        else
                            groupStartHeight[group] = (startPos.Item1, x + itemWidth, y, startPos.Item4 + 1);
                    }
                }
            }
            InvalidateVisual();
            return finalSize;
        }

        private (int, int)? Find(IInputElement element)
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
        
        public IInputElement GetControl(NavigationDirection direction, IInputElement @from, bool wrap)
        {
            IControl control = (IControl)@from;
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
                    return GetElementInDir(index.Value, (-1, 0)) ?? from;
                case NavigationDirection.Right:
                    return GetElementInDir(index.Value, (1, 0)) ?? from;
                case NavigationDirection.Up:
                    return GetElementInDir(index.Value, (0, -1)) ?? from;
                case NavigationDirection.Down:
                    return GetElementInDir(index.Value, (0, 1)) ?? from;
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