using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace AvaloniaStyles.Controls.FastTableView;

public class WdeCellDrawer : ICustomCellDrawer
{
    private static ISolidColorBrush CheckBackgroundBrush = new SolidColorBrush(Color.FromRgb(37, 150, 190));
    private static IPen CheckPen = new Pen(new SolidColorBrush(Colors.White), 3);
    private static IPen ButtonTextPen = new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 1);
    private static IPen ButtonBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 245, 245)), 1);
    private static IPen ButtonBackgroundPen = new Pen(new SolidColorBrush(Colors.White), 0);
    
    public bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell cell, ITableRow row)
    {
        if (cell is not WdeCell wde)
            return false;

        if (wde.Value is string s)
            return false;

        if (wde.Value is long l)
            return false;

        if (wde.Value is double d)
            return false;

        
        if (wde.Value is bool b)
        {
            var size = 20;
            var checkboxRect = new Rect(rect.Center.X - size / 2, rect.Center.Y - size / 2, size, size);
            context.FillRectangle(CheckBackgroundBrush, checkboxRect, 4);
            if (b)
            {
                context.DrawLine(CheckPen, new Point(rect.Center.X - 5, rect.Center.Y), new Point(rect.Center.X - 1, rect.Center.Y + 4));
                context.DrawLine(CheckPen, new Point(rect.Center.X + 5, rect.Center.Y - 4), new Point(rect.Center.X - 2, rect.Center.Y + 3));
            }
            return true;
        }

        if (wde.Value is System.Action)
        {
            rect = rect.Deflate(3);

            context.DrawRectangle(ButtonBackgroundPen.Brush, ButtonBorderPen, rect, 4, 4);

            var state = context.PushClip(rect);
            var ft = new FormattedText("Click", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, ButtonTextPen.Brush);
            ft.MaxTextWidth = float.MaxValue; // rect.Width // we don't want text wrapping so pass float.MaxValue
            ft.MaxTextHeight = rect.Height;
            
            context.DrawText(ft, new Point(rect.Center.X - ft.Width / 2, rect.Center.Y - ft.Height / 2));
            state.Dispose();

            return true;
        }
        
        return false;
    }

    public void DrawRow(DrawingContext context, IFastTableContext table, ITableRow row, Rect rect)
    {
    }
}