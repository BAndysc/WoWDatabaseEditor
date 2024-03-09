using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace AvaloniaStyles.Controls.OptimizedVeryFastTableView;

public partial class VirtualizedVeryFastTableView
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
                return default;
            return new Rect(scrollViewer.Offset.X, scrollViewer.Offset.Y + DrawingStartOffsetY, scrollViewer.Viewport.Width, scrollViewer.Viewport.Height - DrawingStartOffsetY);
        }
    }

    private Exception? lastException;

    public override void Render(DrawingContext context)
    {
        try
        {
            if (lastException != null)
            {
                context.DrawText(new FormattedText("Fatal error", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 16, Brushes.Red)
                { }, new Point(0, 0));            
                context.DrawText(new FormattedText(lastException.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Red)
                {
                    MaxTextHeight = Bounds.Height,
                    MaxTextWidth = Bounds.Width
                }, new Point(0, 24));
            }
            else
               RenderImpl(context);
        }
        catch (Exception e)
        {
            lastException = e;
            LOG.LogError(e);
        }
    }

    public void AutoFitColumnsWidth()
    {
        if (Columns is not { } columns)
            return;
        
        var controller = Controller;
        var defaultFont = TextElement.GetFontFamily(this);
        var defaultTypeface = new Typeface(defaultFont);
        var boldTypeface = new Typeface(defaultFont,default, FontWeight.Bold);
        Span<double> columnWidths = stackalloc double[columns.Count];
        var (firstVisibleIndex, lastVisibleIndex) = GetFirstAndLastVisibleRows();
        for (int i = 0; i < columns.Count; i++)
        {
            var ft = new FormattedText(columns[i].Header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, boldTypeface, 12, Brushes.Black);
            columnWidths[i] = ft.Width + ColumnSpacing * 2 + 20; // this +20 is only for SqlEditor which displays a key icon for primary key columns.
                                                                        // If this control is reused for another editor, then it might be changed, but still pls keep
                                                                        // the space in case of SqlEditor.
        }
        
        for (var rowIndex = firstVisibleIndex; rowIndex <= lastVisibleIndex; rowIndex++)
        {
            for (int cellIndex = 0; cellIndex < columnWidths.Length; ++cellIndex)
            {
                var cellText = controller.GetCellText(rowIndex, cellIndex);
                var ft = new FormattedText(cellText ?? "", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, defaultTypeface, 12, Brushes.Black);
                columnWidths[cellIndex] = Math.Max(columnWidths[cellIndex], ft.Width + ColumnSpacing * 2);
            }
        }

        for (int i = 0; i < columns.Count; i++)
            Columns[i].Width = columnWidths[i];
        InvalidateMeasure();
        InvalidateArrange();
    }

    private (int first, int last) GetFirstAndLastVisibleRows()
    {
        var viewPort = DataViewport;
        var itemsCount = ItemsCount;
        if (itemsCount == 0)
            return (0, -1);
        var firstVisibleIndex = (int)((viewPort.Top - DrawingStartOffsetY) / RowHeight);
        var lastVisibleIndex = (int)((viewPort.Bottom - DrawingStartOffsetY) / RowHeight + 1);
        firstVisibleIndex = Math.Clamp(firstVisibleIndex, 0, itemsCount - 1);
        lastVisibleIndex = Math.Clamp(lastVisibleIndex, 0, itemsCount - 1);
        return (firstVisibleIndex, lastVisibleIndex);
    }
    
    private void RenderImpl(DrawingContext context)
    {
        var font = new Typeface(TextElement.GetFontFamily(this));

        var actualWidth = Bounds.Width;

        var viewPort = DataViewport;
        
        double y = DrawingStartOffsetY;

        Span<double> columnWidths = stackalloc double[Columns?.Count ?? 0];
        if (Columns != null)
        {
            for (int i = 0; i < Columns.Count; i++)
                columnWidths[i] = Columns[i].Width;
        }

        var controller = Controller;

        // we draw only the visible rows
        var selectionIterator = MultiSelection.ContainsIterator;
        var itemsCount = ItemsCount;
        if (itemsCount == 0)
        {
            RenderHeaders(context);
            return;
        }

        var (firstVisibleIndex, lastVisibleIndex) = GetFirstAndLastVisibleRows();
        var odd = (firstVisibleIndex % 2) == 1;
        y += firstVisibleIndex * RowHeight;
        for (var rowIndex = firstVisibleIndex; rowIndex <= lastVisibleIndex; rowIndex++)
        {
            double x = 0;
            var rowRect = new Rect(0, y, actualWidth, RowHeight);

            if (viewPort.Intersects(rowRect))
            {
                // background
                bool isSelected = selectionIterator.Contains(rowIndex);
                context.FillRectangle(
                    isSelected ? (SelectedRowBackground) : (odd ? OddRowBackground : EvenRowBackground),
                    rowRect);

                controller.DrawRow(rowIndex, rowRect, context, this);

                var textColor = isSelected ? FocusTextBrush : TextBrush;

                for (int cellIndex = 0; cellIndex < columnWidths.Length; ++cellIndex)
                {
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
                        try
                        {
                            if (!controller.Draw(rowIndex, cellIndex, context, this, ref rect))
                            {
                                var text = controller.GetCellText(rowIndex, cellIndex);
                                if (!string.IsNullOrEmpty(text))
                                {
                                    var indexOfEndOfLine = text.IndexOf('\n');
                                    if (indexOfEndOfLine != -1)
                                        text = text.Substring(0, indexOfEndOfLine);

                                    rect = rect.WithWidth(rect.Width - ColumnSpacing);
                                    var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, 12, textColor)
                                    {
                                        MaxTextHeight = RowHeight,
                                        MaxTextWidth = float.MaxValue // rect.Width // we don't want text wrapping so pass float.MaxValue
                                    };
                                    
                                    if (Math.Abs(rectWidth - rect.Width) > 0.01)
                                    {
                                        state.Dispose();
                                        state = context.PushClip(rect);
                                    }
                                    context.DrawText(ft, new Point(rect.X + ColumnSpacing, rect.Center.Y - ft.Height / 2));
                                }
                            }
                        }
                        finally
                        {
                            state.Dispose();
                        }
                    }

                    x += columnWidth;
                }
            }

            y += RowHeight;
            odd = !odd;
        }

        if (IsKeyboardFocusWithin && IsSelectedCellValid && !editor.IsOpened)
        {
            context.DrawRectangle(FocusOuterPen, SelectedCellRect, 4);
            context.DrawRectangle(FocusPen, SelectedCellRect, 4);
        }

        RenderHeaders(context);
    }

    private void RenderHeaders(DrawingContext context)
    {
        if (Columns == null)
            return;
        
        var controller = Controller;
        
        FontFamily font = FontFamily.Default;
        if (Application.Current!.Styles.TryGetResource("MainFontSans", SystemTheme.EffectiveThemeVariant, out var mainFontSans) && mainFontSans is FontFamily mainFontSansFamily)
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
            var ft = new FormattedText(column.Header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(font, FontStyle.Normal, FontWeight.Bold), 12, BorderPen.Brush)
            {
                MaxTextHeight = RowHeight,
                MaxTextWidth = float.MaxValue // column.Width // we don't want text wrapping so pass float.MaxValue
            };
            
            bool isMouseOverColumn = isMouseOverHeader && lastMouseLocation.X >= x && lastMouseLocation.X < x + column.Width;
            if (isMouseOverColumn && interactiveHeader)
                context.FillRectangle(lastMouseButtonPressed && !isMouseOverSplitter ? HeaderPressedBackground : HeaderHoverBackground, new Rect(x, y, column.Width, RowHeight));

            var cellRect = new Rect(x + ColumnSpacing, y, Math.Max(0, column.Width - ColumnSpacing * 2), RowHeight);
            controller.DrawHeader(i, context, this, ref cellRect);
            var state = context.PushClip(cellRect);
            context.DrawText(ft, new Point(cellRect.X, cellRect.Center.Y - ft.Height / 2));
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
    
    private int GetRowIndexByY(double mouseY)
    {
        int index = (int)((mouseY - DrawingStartOffsetY) / RowHeight);
        if (IsRowIndexValid(index))
            return index;
        return -1;
    }
}
