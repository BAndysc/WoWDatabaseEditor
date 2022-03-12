using Avalonia;
using Avalonia.Media;
using AvaloniaStyles.Controls.FastTableView;
using WDE.DatabaseEditors.ViewModels.SingleRow;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow;

public class CustomCellDrawer : ICustomCellDrawer
{
    private static IPen ModifiedCellPen = new Pen(Brushes.Red, 1);

    public void DrawRow(DrawingContext context, ITableRow r, Rect rect)
    {
        if (r is not DatabaseEntityViewModel row)
            return;

        if (!row.Entity.ExistInDatabase)
        {
            context.FillRectangle(ModifiedCellPen.Brush, rect.WithWidth(5));
            context.DrawLine(ModifiedCellPen, rect.TopLeft,rect.TopRight);
            context.DrawLine(ModifiedCellPen, rect.BottomLeft, rect.BottomRight);
        }
    }

    public bool Draw(DrawingContext context, Rect rect, ITableCell c)
    {
        if (c is not SingleRecordDatabaseCellViewModel cell)
            return false;

        if (cell.IsModified && cell.Parent.Entity.ExistInDatabase)
        {
            context.DrawRectangle(null, ModifiedCellPen, rect);
            // don't return true, because we want to draw original value anyway
        }
        
        return false;
    }
}