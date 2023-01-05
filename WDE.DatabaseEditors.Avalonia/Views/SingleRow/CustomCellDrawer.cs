using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Parameters;
using WDE.DatabaseEditors.Avalonia.Services;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.SingleRow;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow;

public abstract class CustomCellDrawerInteractorBase
{
    protected Rect GetThreeDotRectForCell(Rect rect)
    {
        return new Rect(rect.Right - 32, rect.Y, 32, rect.Height);
    }
}

public class CustomCellDrawer : CustomCellDrawerInteractorBase, ICustomCellDrawer
{
    private static IPen ModifiedCellPen = new Pen(Brushes.Red, 1);
    private static IPen PhantomRowPen = new Pen(Brushes.Orange, 1);
    private static IPen ButtonTextPen = new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 1);
    private static IPen ButtonBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 245, 245)), 1);
    private static IPen ButtonBackgroundPen = new Pen(new SolidColorBrush(Colors.White), 0);
    private static IPen ButtonBackgroundHoverPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 245, 245)), 0);
    private static IPen ButtonBackgroundPressedPen = new Pen(new SolidColorBrush(Color.FromRgb(215, 215, 215)), 0);
    private static IPen ButtonBackgroundDisabledPen = new Pen(new SolidColorBrush(Color.FromRgb(170, 170, 170)), 0);
    
    private Point mouseCursor;
    private bool leftPressed;
    
    static CustomCellDrawer()
    {
        ModifiedCellPen.GetResource("FastTableView.ModifiedCellPen", ModifiedCellPen, out ModifiedCellPen);
        PhantomRowPen.GetResource("FastTableView.PhantomRowPen", PhantomRowPen, out PhantomRowPen);
        ButtonTextPen.GetResource("FastTableView.ButtonTextPen", ButtonTextPen, out ButtonTextPen);
        ButtonBorderPen.GetResource("FastTableView.ButtonBorderPen", ButtonBorderPen, out ButtonBorderPen);
        ButtonBackgroundPen.GetResource("FastTableView.ButtonBackgroundPen", ButtonBackgroundPen, out ButtonBackgroundPen);
        ButtonBackgroundHoverPen.GetResource("FastTableView.ButtonBackgroundHoverPen", ButtonBackgroundHoverPen, out ButtonBackgroundHoverPen);
        ButtonBackgroundPressedPen.GetResource("FastTableView.ButtonBackgroundPressedPen", ButtonBackgroundPressedPen, out ButtonBackgroundPressedPen);
        ButtonBackgroundDisabledPen.GetResource("FastTableView.ButtonBackgroundDisabledPen", ButtonBackgroundDisabledPen, out ButtonBackgroundDisabledPen);
    }

    public void DrawRow(DrawingContext context, ITableRow r, Rect rect)
    {
        if (r is not DatabaseEntityViewModel row)
            return;

        if (!row.Entity.ExistInDatabase)
        {
            var pen = row.IsPhantomEntity ? PhantomRowPen : ModifiedCellPen;
            context.FillRectangle(pen.Brush, rect.WithWidth(5));
            context.DrawLine(pen, rect.TopLeft,rect.TopRight);
            context.DrawLine(pen, rect.BottomLeft, rect.BottomRight);
        }
    }

    public bool UpdateCursor(Point point, bool leftPressed)
    {
        mouseCursor = point;
        this.leftPressed = leftPressed;
        return true;
    }

    private void DrawButton(DrawingContext context, Rect rect, string text, int margin, bool enabled = true)
    {
        rect = rect.Deflate(margin);

        bool isOver = rect.Contains(mouseCursor);
            
        context.DrawRectangle((!enabled ? ButtonBackgroundDisabledPen : isOver ? (leftPressed ? ButtonBackgroundPressedPen : ButtonBackgroundHoverPen) : ButtonBackgroundPen).Brush, ButtonBorderPen, rect, 4, 4);

        if (string.IsNullOrEmpty(text))
            return;
        
        var state = context.PushClip(rect);
        if (char.IsAscii(text[0]))
        {
            var ft = new FormattedText
            {
                Text = text,
                Constraint = new Size(rect.Width, rect.Height),
                Typeface = Typeface.Default,
                FontSize = 12
            };
            context.DrawText(ButtonTextPen.Brush, new Point(rect.Center.X - ft.Bounds.Width / 2, rect.Center.Y - ft.Bounds.Height / 2), ft);
        }
        else
        {
            var tl = new TextLayout(text, Typeface.Default, 12, ButtonTextPen.Brush, TextAlignment.Center, maxWidth: rect.Width, maxHeight:rect.Height);
            tl.Draw(context, new Point(rect.Left, rect.Center.Y - tl.Size.Height / 2));
        }
        state.Dispose();
    }

    public bool Draw(DrawingContext context, ref Rect rect, ITableCell c)
    {
        if (c is not SingleRecordDatabaseCellViewModel cell)
            return false;

        if (cell.ActionCommand != null)
        {
            DrawButton(context, rect, cell.ActionLabel, 3, cell.ActionCommand.CanExecute(null));
            return true;
        }
        
        if (cell.IsModified && cell.Parent.Entity.ExistInDatabase)
        {
            context.DrawRectangle(null, ModifiedCellPen, rect);
            // don't return true, because we want to draw original value anyway
        }

        if (cell.HasItems && !cell.UseFlagsPicker && !cell.UseItemPicker)
        {
            if (rect.Contains(mouseCursor))
            {
                var threeDotRect = GetThreeDotRectForCell(rect);
                DrawButton(context, threeDotRect, "...", 3);
                rect = rect.Deflate(new Thickness(0, 0, threeDotRect.Width, 0));
            }
        }
        
        if (cell.ParameterValue?.BaseParameter is IItemParameter && cell.TableField is DatabaseField<long> longField)
        {
            var icons = ViewBind.ResolveViewModel<IItemIconsService>();
            var icn = icons.GetIcon((uint)longField.Current.Value);
            if (icn != null)
            {
                context.DrawImage(icn, new Rect(rect.X, rect.Center.Y - 18/2, 18, 18));
            }
            rect = rect.Deflate(new Thickness(20, 0, 0, 0));
        }

        return false;
    }
}