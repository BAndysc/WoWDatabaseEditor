using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    /// <summary>
    /// Viewport rect WITHOUT the header
    /// </summary>
    private Rect DataViewport
    {
        get
        {
            var scrollViewer = ScrollViewer;
            if (scrollViewer == null)
                return Rect.Empty;
            return new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y + DrawingStartOffsetY, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height - DrawingStartOffsetY);
        }
    }

    private bool IsFilteredRowVisible(ITableRowGroup group, ITableRow row, IRowFilterPredicate? filter, object? parameter)
    {
        if (filter == null)
            return group.IsExpanded;

        return group.IsExpanded && filter.IsVisible(row, parameter);
    }


    private bool IsFilteredRowVisible(ITableRowGroup group, ITableRow row) => IsFilteredRowVisible(group, row, RowFilter, RowFilterParameter);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Items == null)
            return;

        var font = new Typeface(TextBlock.GetFontFamily(this));
        
        var actualWidth = Bounds.Width;

        var viewPort = DataViewport;
        
        double y = DrawingStartOffsetY;
        bool odd = false;

        Span<double> columnWidths = stackalloc double[Columns?.Count ?? 0];
        if (Columns != null)
        {
            for (int i = 0; i < Columns.Count; i++)
                columnWidths[i] = Columns[i].Width;
        }

        var cellDrawer = CustomCellDrawer;
        var rowFiler = RowFilter;
        var rowFilterParameter = RowFilterParameter;

        // we draw only the visible rows
        DrawingContext.PushedState? viewPortClip = IsGroupingEnabled ? context.PushClip(viewPort.Deflate(new Thickness(0, lastStickyHeaderHeight, 0, 0))) : null;
        var selectionIterator = MultiSelection.ContainsIterator;
        int groupIndex = 0;
        for (var index = 0; index < Items.Count; index++)
        {
            var group = Items[index];
            var groupStartY = y;
            var headerHeight = GetTotalHeaderHeight(index);
            var groupHeight = headerHeight;
            int visibleRowsCount = 0;
            if (group.IsExpanded)
            {
                if (rowFiler == null)
                {
                    visibleRowsCount = group.Rows.Count;
                }
                else
                {
                    foreach (var row in group.Rows)
                    {
                        if (IsFilteredRowVisible(group, row, rowFiler, rowFilterParameter))
                            visibleRowsCount++;
                    }
                }

                groupHeight += visibleRowsCount * RowHeight;
            }

            // out of bounds in the upper part
            if (groupStartY + groupHeight < DataViewport.Top)
            {
                if (group.Rows.Count % 2 == 1)
                    odd = !odd;
                y += groupHeight;
            }
            else if (groupStartY > DataViewport.Bottom) // below
            {
                break;
            }
            else
            {
                y += headerHeight;
                int rowIndex = -1; // we start at -1, because we add 1 in the beginning of the loop
                foreach (var row in group.Rows)
                {
                    rowIndex += 1;

                    if (!IsFilteredRowVisible(group, row, rowFiler, rowFilterParameter))
                        continue;

                    double x = 0;
                    var rowRect = new Rect(0, y, actualWidth, RowHeight);

                    if (viewPort.Intersects(rowRect))
                    {
                        // background
                        bool isSelected = selectionIterator.Contains(new VerticalCursor(groupIndex, rowIndex));
                        context.FillRectangle(
                            isSelected ? (SelectedRowBackground) : (odd ? OddRowBackground : EvenRowBackground),
                            rowRect);

                        cellDrawer?.DrawRow(context, this, row, rowRect);

                        var textColor = isSelected ? FocusTextBrush : TextBrush;

                        int cellIndex = 0;
                        foreach (var cell in row.CellsList)
                        {
                            if (cellIndex >= columnWidths.Length)
                                continue;

                            if (!IsColumnVisible(cellIndex))
                            {
                                cellIndex++;
                                continue;
                            }

                            var columnWidth = columnWidths[cellIndex];

                            // we draw only the visible columns
                            // todo: could be optimized so we don't iterate through all columns when we know we don't need to
                            if (x + columnWidth > DataViewport.Left && x < DataViewport.Right)
                            {
                                var rect = new Rect(x, y, columnWidth, RowHeight);
                                var rectWidth = rect.Width;
                                var state = context.PushClip(rect);
                                if (cellDrawer == null || !cellDrawer.Draw(context, this, ref rect, cell))
                                {
                                    var text = cell.ToString();
                                    if (!string.IsNullOrEmpty(text))
                                    {
                                        var indexOfEndOfLine = text.IndexOf('\n');
                                        if (indexOfEndOfLine != -1)
                                            text = text.Substring(0, indexOfEndOfLine);

                                        rect = rect.WithWidth(rect.Width - ColumnSpacing);
                                        var ft = new FormattedText
                                        {
                                            Text = text,
                                            Constraint = new Size(rect.Width, RowHeight),
                                            Typeface = font,
                                            FontSize = 12
                                        };
                                        if (Math.Abs(rectWidth - rect.Width) > 0.01)
                                        {
                                            state.Dispose();
                                            state = context.PushClip(rect);
                                        }

                                        context.DrawText(textColor,
                                            new Point(rect.X + ColumnSpacing, y + RowHeight / 2 - ft.Bounds.Height / 2),
                                            ft);
                                    }
                                }

                                state.Dispose();
                            }

                            x += columnWidth;
                            cellIndex++;
                        }
                    }

                    y += RowHeight;
                    odd = !odd;
                }
            }

            groupIndex++;
        }

        if (IsKeyboardFocusWithin && IsSelectedCellValid && !editor.IsOpened)
        {
            context.DrawRectangle(FocusOuterPen, SelectedCellRect, 4);
            context.DrawRectangle(FocusPen, SelectedCellRect, 4);
        }
        if (viewPortClip.HasValue)
            viewPortClip.Value.Dispose();

        RenderHeaders(context);
    }

    private void RenderHeaders(DrawingContext context)
    {
        if (Columns == null)
            return;
        
        FontFamily font = FontFamily.Default;
        if (Application.Current!.Styles.TryGetResource("MainFontSans", out var mainFontSans) && mainFontSans is FontFamily mainFontSansFamily)
            font = mainFontSansFamily;

        var scrollViewer = ScrollViewer;
        if (scrollViewer == null)
            return;
        
        double y = scrollViewer.Offset.Y;
        double height = RowHeight;
        double x = 0;
        
        // draw the background
        context.DrawRectangle(HeaderBackground, null, new Rect(scrollViewer.Offset.X, y, scrollViewer.Viewport.Width, height));

        // small todo: we could optimize this by only drawing the visible columns
        var isMouseOverHeader = IsPointHeader(lastMouseLocation);
        var isMouseOverSplitter = IsOverColumnSplitter(lastMouseLocation.X, out var columnSplitterIndex);
        var interactiveHeader = InteractiveHeader;
        for (int i = 0; i < Columns.Count; ++i)
        {
            if (!IsColumnVisible(i))
                continue;
            
            var column = Columns[i];
            var ft = new FormattedText
            {
                Text = column.Header,
                Constraint = new Size(column.Width, RowHeight),
                Typeface = new Typeface(font, FontStyle.Normal, FontWeight.Bold),
                FontSize = 12
            };
            
            bool isMouseOverColumn = isMouseOverHeader && lastMouseLocation.X >= x && lastMouseLocation.X < x + column.Width;
            if (isMouseOverColumn && interactiveHeader)
                context.FillRectangle(lastMouseButtonPressed && !isMouseOverSplitter ? HeaderPressedBackground : HeaderHoverBackground, new Rect(x, y, column.Width, RowHeight));
            
            var state = context.PushClip(new Rect(x + ColumnSpacing, y, Math.Max(0, column.Width - ColumnSpacing * 2), RowHeight));
            context.DrawText(BorderPen.Brush, new Point(x + ColumnSpacing, y + RowHeight / 2 - ft.Bounds.Height / 2), ft);
            state.Dispose();
            
            x += column.Width;
            
            if (i != Columns.Count - 1) // if not last
                context.DrawLine(HeaderBorderBackground, new Point(x, y), new Point(x, y + scrollViewer.Viewport.Height));
        }
        
        // draw the bottom border
        context.DrawLine(HeaderBorderBackground, new Point(scrollViewer.Offset.X, y + height - 1), new Point(scrollViewer.Offset.X + scrollViewer.Viewport.Width, y + height - 1));
    }

    private bool IsPointHeader(Point p)
    {
        if (ScrollViewer == null)
            return false;
        return p.Y < RowHeight + ScrollViewer.Offset.Y;
    }
    
    private double GetColumnWidth(int index) => Columns != null && index >= 0 && index < Columns.Count ? Columns[index].Width : 0;

    private (double x, double width) GetColumnRect(int index)
    {
        if (Columns == null || index < 0 || index >= Columns.Count)
            return (0, 0);
        return (Columns.Take(index).Where(c => c.IsVisible).Select(c => c.Width).Sum(), Columns[index].Width);
    }

    private int? GetColumnIndexByX(double x)
    {
        if (Columns == null)
            return null;
        double accumulator = 0;
        for (int i = 0; i < Columns.Count; ++i)
        {
            if (!Columns[i].IsVisible)
                continue;
            
            accumulator += Columns[i].Width;
            if (x < accumulator)
                return i;
        }

        return null;
    }
    
    private bool IsColumnVisible(int index)
    {
        if (index < 0 || index >= columnVisibility.Count)
            return true;
        return columnVisibility[index];
    }
    
    private VerticalCursor? GetRowIndexByY(double mouseY)
    {
        if (Items == null)
            return null;
        double y = DrawingStartOffsetY;
        var groupIndex = 0;

        var rowFilter = RowFilter;
        var rowFilterParameter = RowFilterParameter;

        for (var index = 0; index < Items.Count; index++)
        {
            var group = Items[index];
            var groupStartY = y;
            var groupHeight = GetTotalHeaderHeight(index);

            if (group.IsExpanded)
            {
                if (rowFilter == null)
                    groupHeight += RowHeight * group.Rows.Count;
                else
                {
                    foreach (var row in group.Rows)
                    {
                        if (IsFilteredRowVisible(group, row, rowFilter, rowFilterParameter))
                            groupHeight += RowHeight;
                    }
                }
            }

            if (groupStartY + groupHeight < mouseY)
                y += groupHeight;
            else
            {
                y += GetTotalHeaderHeight(index);

                var rowIndex = -1;
                foreach (var row in group.Rows)
                {
                    rowIndex++;

                    if (!IsFilteredRowVisible(group, row, rowFilter, rowFilterParameter))
                        continue;

                    var rowEnd = y + RowHeight;
                    if (mouseY >= y && mouseY < rowEnd)
                        return new VerticalCursor(groupIndex, rowIndex);
                    y = rowEnd;
                }
            }

            groupIndex++;
        }

        return null;
    }
}