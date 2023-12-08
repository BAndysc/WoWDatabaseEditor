using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using WDE.Common.Parameters;
using WDE.Common.Utils;

namespace AvaloniaStyles.Controls.FastTableView;

public interface ICustomCellDrawer
{
    bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell cell);
    void DrawRow(DrawingContext context, IFastTableContext table, ITableRow row, Rect rect);
    bool UpdateCursor(Point point, bool leftPressed) { return false; }
}

public interface IFastTableContext
{
    void InvalidateVisual();
}

public abstract class BaseCustomCellDrawer : ICustomCellDrawer
{
    public abstract bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell cell);
    public abstract void DrawRow(DrawingContext context, IFastTableContext table, ITableRow row, Rect rect);
    
    private static IPen ModifiedCellPen = new Pen(Brushes.Red, 1);
    private static IPen PhantomRowPen = new Pen(Brushes.Orange, 1);
    private static IPen ButtonTextPen = new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 1);
    private static IPen ButtonBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 245, 245)), 1);
    private static IPen ButtonBackgroundPen = new Pen(new SolidColorBrush(Colors.White), 0);
    private static IPen ButtonBackgroundHoverPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 245, 245)), 0);
    private static IPen ButtonBackgroundPressedPen = new Pen(new SolidColorBrush(Color.FromRgb(215, 215, 215)), 0);
    private static IPen ButtonBackgroundDisabledPen = new Pen(new SolidColorBrush(Color.FromRgb(170, 170, 170)), 0);
    private static ISolidColorBrush TextBrush = new SolidColorBrush(Color.FromRgb(41, 41, 41));
    private static ISolidColorBrush FocusTextBrush = new SolidColorBrush(Colors.White);
    private static FontFamily MonoFont = FontFamily.Default;

    protected Point mouseCursor;
    protected bool leftPressed;
    
    static BaseCustomCellDrawer()
    {
        ModifiedCellPen.GetResource("FastTableView.ModifiedCellPen", ModifiedCellPen, out ModifiedCellPen);
        PhantomRowPen.GetResource("FastTableView.PhantomRowPen", PhantomRowPen, out PhantomRowPen);
        ButtonTextPen.GetResource("FastTableView.ButtonTextPen", ButtonTextPen, out ButtonTextPen);
        ButtonBorderPen.GetResource("FastTableView.ButtonBorderPen", ButtonBorderPen, out ButtonBorderPen);
        ButtonBackgroundPen.GetResource("FastTableView.ButtonBackgroundPen", ButtonBackgroundPen, out ButtonBackgroundPen);
        ButtonBackgroundHoverPen.GetResource("FastTableView.ButtonBackgroundHoverPen", ButtonBackgroundHoverPen, out ButtonBackgroundHoverPen);
        ButtonBackgroundPressedPen.GetResource("FastTableView.ButtonBackgroundPressedPen", ButtonBackgroundPressedPen, out ButtonBackgroundPressedPen);
        ButtonBackgroundDisabledPen.GetResource("FastTableView.ButtonBackgroundDisabledPen", ButtonBackgroundDisabledPen, out ButtonBackgroundDisabledPen);
        TextBrush.GetResource("FastTableView.TextBrush", TextBrush, out TextBrush);
        FocusTextBrush.GetResource("FastTableView.FocusTextBrush", FocusTextBrush, out FocusTextBrush);
        MonoFont.GetResource("MonoFont", MonoFont, out MonoFont);
    }

    public bool UpdateCursor(Point point, bool leftPressed)
    {
        mouseCursor = point;
        this.leftPressed = leftPressed;
        return true;
    }

    protected void DrawText(DrawingContext context, Rect rect, string text, Color? color = null, float opacity = 0.4f)
    {
        var textColor = new SolidColorBrush(color ?? TextBrush.Color, opacity);
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(MonoFont), 12, textColor)
        {
            MaxTextHeight = rect.Height,
            MaxTextWidth = rect.Width
        };
        context.DrawText(ft, new Point(rect.X, rect.Center.Y - ft.Height / 2));
    }

    protected void DrawButton(DrawingContext context, Rect rect, string text, int margin, bool enabled = true)
    {
        rect = rect.Deflate(margin);

        bool isOver = rect.Contains(mouseCursor);
            
        context.DrawRectangle((!enabled ? ButtonBackgroundDisabledPen : isOver ? (leftPressed ? ButtonBackgroundPressedPen : ButtonBackgroundHoverPen) : ButtonBackgroundPen).Brush, ButtonBorderPen, rect, 4, 4);

        if (string.IsNullOrEmpty(text))
            return;
        
        var state = context.PushClip(rect);
        if (char.IsAscii(text[0]))
        {
            var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, ButtonTextPen.Brush)
            {
                MaxTextHeight = rect.Height,
                MaxTextWidth = rect.Width
            };
            context.DrawText(ft, new Point(rect.Center.X - ft.Width / 2, rect.Center.Y - ft.Height / 2));
        }
        else
        {
            var tl = new TextLayout(text, Typeface.Default, 12, ButtonTextPen.Brush, TextAlignment.Center, maxWidth: rect.Width, maxHeight:rect.Height);
            tl.Draw(context, new Point(rect.Left, rect.Center.Y - tl.Height / 2));
        }
        state.Dispose();
    }
}