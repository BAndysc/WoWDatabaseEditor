using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaStyles.Controls.FastTableView;

public partial class VeryFastTableView
{
    protected override Size MeasureOverride(Size availableSize)
    {
        availableSize = availableSize.WithHeight((Rows?.Count ?? 0) * RowHeight + DrawingStartOffsetY);
        availableSize = availableSize.WithWidth(0);
        if (Columns != null)
        {
            availableSize = availableSize.WithWidth(Columns.Where(c=>c.IsVisible).Select(c => c.Width).Sum());
        }
        return availableSize;
    }

    public override void Render(DrawingContext context)
    {
        if (Rows == null)
            return;

        var font = new Typeface(TextBlock.GetFontFamily(this));
        
        var actualWidth = Bounds.Width;

        var scrollViewer = this.FindAncestorOfType<ScrollViewer>();

        // determine the first and last visible row
        var startIndex = Math.Max(0, (int)(scrollViewer.Offset.Y / RowHeight) - 1);
        var endIndex = Math.Min(startIndex + scrollViewer.Viewport.Height / RowHeight + 2, Rows.Count);

        double y = startIndex * RowHeight + DrawingStartOffsetY;
        bool odd = startIndex % 2 == 0;
        
        // we draw only the visible rows
        for (var index = startIndex; index < endIndex; index++)
        {
            var row = Rows[index];
            double x = 0;
            var rowRect = new Rect(0, y, actualWidth, RowHeight);
            
            // background
            context.FillRectangle(SelectedRowIndex == index ? (SelectedRowBackground) : (odd ? OddRowBackground : EvenRowBackground), rowRect);
            
            var textColor = index == SelectedRowIndex ? FocusTextBrush : TextBrush;
            
            int cellIndex = 0;
            foreach (var cell in row.CellsList)
            {
                if (!IsColumnVisible(cellIndex))
                {
                    cellIndex++;
                    continue;
                }
                
                var columnWidth = GetColumnWidth(cellIndex);

                // we draw only the visible columns
                // todo: could be optimized so we don't iterate through all columns when we know we don't need to
                if (x + columnWidth > scrollViewer.Offset.X && x < scrollViewer.Offset.X + scrollViewer.Viewport.Width)
                {
                    var rect = new Rect(x, y, Math.Max(0, columnWidth - ColumnSpacing), RowHeight);
                    var state = context.PushClip(rect);
                    if (CustomCellDrawer == null || !CustomCellDrawer.Draw(context, rect, cell))
                    {
                        var ft = new FormattedText
                        {
                            Text = cell.ToString(),
                            Constraint = new Size(columnWidth - ColumnSpacing, RowHeight),
                            Typeface = font,
                            FontSize = 12
                        };
                        context.DrawText(textColor, new Point(x + ColumnSpacing, y + RowHeight / 2 - ft.Bounds.Height / 2), ft);
                    }
                    
                    state.Dispose();

                }

                x += columnWidth;
                cellIndex++;
            }
            
            y += RowHeight;
            odd = !odd;
        }

        if (IsSelectedCellValid)
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
        
        FontFamily font = FontFamily.Default;
        if (Application.Current.Styles.TryGetResource("MainFontSans", out var mainFontSans) && mainFontSans is FontFamily mainFontSansFamily)
            font = mainFontSansFamily;

        var scrollViewer = ScrollViewer;
        double y = scrollViewer.Offset.Y;
        double height = RowHeight;
        double x = 0;
        
        // draw the background
        context.DrawRectangle(HeaderBackground, null, new Rect(scrollViewer.Offset.X, y, scrollViewer.Viewport.Width, height));
        context.DrawLine(HeaderBorderBackground, new Point(scrollViewer.Offset.X, y + height), new Point(scrollViewer.Offset.X + scrollViewer.Viewport.Width, y + height));
        
        // small todo: we could optimize this by only drawing the visible columns
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
            var state = context.PushClip(new Rect(x + ColumnSpacing, y, Math.Max(0, column.Width - ColumnSpacing * 2), RowHeight));
            context.DrawText(BorderPen.Brush, new Point(x + ColumnSpacing, y + RowHeight / 2 - ft.Bounds.Height / 2), ft);
            state.Dispose();
            
            x += column.Width;
            
            if (i != Columns.Count - 1) // if not last
                context.DrawLine(HeaderBorderBackground, new Point(x, y), new Point(x, y + height));
        }
    }

    private bool IsPointHeader(Point p)
    {
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
    
    private int? GetRowIndexByY(double y)
    {
        return (int)((y - DrawingStartOffsetY) / RowHeight);
    }
}