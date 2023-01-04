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
    bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell cell, ITableRow row);
    void DrawRow(DrawingContext context, IFastTableContext table, ITableRow row, Rect rect);
    bool UpdateCursor(Point point, bool leftPressed) { return false; }
}

public interface IFastTableContext
{
    void InvalidateVisual();
}

public static class TableExtensions
{
    public static Rect GetThreeDotRectForCell(this Rect rect)
    {
        return new Rect(rect.Right - 32, rect.Y, 32, rect.Height);
    }
}

public abstract class BaseCustomCellDrawer : ICustomCellDrawer
{
    public abstract bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell cell, ITableRow row);
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
    private static ISolidColorBrush CheckBoxCheckBackgroundFillUnchecked = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundFillUncheckedPointerOver = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundFillUncheckedPressed = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundFillUncheckedDisabled = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundFillChecked = new SolidColorBrush(Color.FromRgb(40, 120, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundFillCheckedPointerOver = new SolidColorBrush(Color.FromRgb(40, 120, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundFillCheckedPressed = new SolidColorBrush(Color.FromRgb(40, 120, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundFillCheckedDisabled = new SolidColorBrush(Color.FromRgb(40, 120, 245));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeUnchecked = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeUncheckedPointerOver = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeUncheckedPressed = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeUncheckedDisabled = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeChecked = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeCheckedPointerOver = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeCheckedPressed = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckBackgroundStrokeCheckedDisabled = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundUnchecked = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundUncheckedPointerOver = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundUncheckedPressed = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundUncheckedDisabled = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundChecked = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundCheckedPointerOver = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundCheckedPressed = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundCheckedDisabled = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundIndeterminate = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundIndeterminatePointerOver = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundIndeterminatePressed = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    private static ISolidColorBrush CheckBoxCheckGlyphForegroundIndeterminateDisabled = new SolidColorBrush(Color.FromRgb(245, 245, 245));
    
    private static IPen CheckBoxCheckBackgroundFillUncheckedPen;
    private static IPen CheckBoxCheckBackgroundFillUncheckedPointerOverPen;
    private static IPen CheckBoxCheckBackgroundFillUncheckedPressedPen;
    private static IPen CheckBoxCheckBackgroundFillUncheckedDisabledPen;
    private static IPen CheckBoxCheckBackgroundFillCheckedPen;
    private static IPen CheckBoxCheckBackgroundFillCheckedPointerOverPen;
    private static IPen CheckBoxCheckBackgroundFillCheckedPressedPen;
    private static IPen CheckBoxCheckBackgroundFillCheckedDisabledPen;
    private static IPen CheckBoxCheckBackgroundStrokeUncheckedPen;
    private static IPen CheckBoxCheckBackgroundStrokeUncheckedPointerOverPen;
    private static IPen CheckBoxCheckBackgroundStrokeUncheckedPressedPen;
    private static IPen CheckBoxCheckBackgroundStrokeUncheckedDisabledPen;
    private static IPen CheckBoxCheckBackgroundStrokeCheckedPen;
    private static IPen CheckBoxCheckBackgroundStrokeCheckedPointerOverPen;
    private static IPen CheckBoxCheckBackgroundStrokeCheckedPressedPen;
    private static IPen CheckBoxCheckBackgroundStrokeCheckedDisabledPen;
    private static IPen CheckBoxCheckGlyphForegroundUncheckedPen;
    private static IPen CheckBoxCheckGlyphForegroundUncheckedPointerOverPen;
    private static IPen CheckBoxCheckGlyphForegroundUncheckedPressedPen;
    private static IPen CheckBoxCheckGlyphForegroundUncheckedDisabledPen;
    private static IPen CheckBoxCheckGlyphForegroundCheckedPen;
    private static IPen CheckBoxCheckGlyphForegroundCheckedPointerOverPen;
    private static IPen CheckBoxCheckGlyphForegroundCheckedPressedPen;
    private static IPen CheckBoxCheckGlyphForegroundCheckedDisabledPen;
    private static IPen CheckBoxCheckGlyphForegroundIndeterminatePen;
    private static IPen CheckBoxCheckGlyphForegroundIndeterminatePointerOverPen;
    private static IPen CheckBoxCheckGlyphForegroundIndeterminatePressedPen;
    private static IPen CheckBoxCheckGlyphForegroundIndeterminateDisabledPen;
    
    
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
        CheckBoxCheckBackgroundFillUnchecked.GetResource("CheckBoxCheckBackgroundFillUnchecked", CheckBoxCheckBackgroundFillUnchecked, out CheckBoxCheckBackgroundFillUnchecked);
        CheckBoxCheckBackgroundFillUncheckedPointerOver.GetResource("CheckBoxCheckBackgroundFillUncheckedPointerOver", CheckBoxCheckBackgroundFillUncheckedPointerOver, out CheckBoxCheckBackgroundFillUncheckedPointerOver);
        CheckBoxCheckBackgroundFillUncheckedPressed.GetResource("CheckBoxCheckBackgroundFillUncheckedPressed", CheckBoxCheckBackgroundFillUncheckedPressed, out CheckBoxCheckBackgroundFillUncheckedPressed);
        CheckBoxCheckBackgroundFillUncheckedDisabled.GetResource("CheckBoxCheckBackgroundFillUncheckedDisabled", CheckBoxCheckBackgroundFillUncheckedDisabled, out CheckBoxCheckBackgroundFillUncheckedDisabled);
        CheckBoxCheckBackgroundFillChecked.GetResource("CheckBoxCheckBackgroundFillChecked", CheckBoxCheckBackgroundFillChecked, out CheckBoxCheckBackgroundFillChecked);
        CheckBoxCheckBackgroundFillCheckedPointerOver.GetResource("CheckBoxCheckBackgroundFillCheckedPointerOver", CheckBoxCheckBackgroundFillCheckedPointerOver, out CheckBoxCheckBackgroundFillCheckedPointerOver);
        CheckBoxCheckBackgroundFillCheckedPressed.GetResource("CheckBoxCheckBackgroundFillCheckedPressed", CheckBoxCheckBackgroundFillCheckedPressed, out CheckBoxCheckBackgroundFillCheckedPressed);
        CheckBoxCheckBackgroundFillCheckedDisabled.GetResource("CheckBoxCheckBackgroundFillCheckedDisabled", CheckBoxCheckBackgroundFillCheckedDisabled, out CheckBoxCheckBackgroundFillCheckedDisabled);
        CheckBoxCheckBackgroundStrokeUnchecked.GetResource("CheckBoxCheckBackgroundStrokeUnchecked", CheckBoxCheckBackgroundStrokeUnchecked, out CheckBoxCheckBackgroundStrokeUnchecked);
        CheckBoxCheckBackgroundStrokeUncheckedPointerOver.GetResource("CheckBoxCheckBackgroundStrokeUncheckedPointerOver", CheckBoxCheckBackgroundStrokeUncheckedPointerOver, out CheckBoxCheckBackgroundStrokeUncheckedPointerOver);
        CheckBoxCheckBackgroundStrokeUncheckedPressed.GetResource("CheckBoxCheckBackgroundStrokeUncheckedPressed", CheckBoxCheckBackgroundStrokeUncheckedPressed, out CheckBoxCheckBackgroundStrokeUncheckedPressed);
        CheckBoxCheckBackgroundStrokeUncheckedDisabled.GetResource("CheckBoxCheckBackgroundStrokeUncheckedDisabled", CheckBoxCheckBackgroundStrokeUncheckedDisabled, out CheckBoxCheckBackgroundStrokeUncheckedDisabled);
        CheckBoxCheckBackgroundStrokeChecked.GetResource("CheckBoxCheckBackgroundStrokeChecked", CheckBoxCheckBackgroundStrokeChecked, out CheckBoxCheckBackgroundStrokeChecked);
        CheckBoxCheckBackgroundStrokeCheckedPointerOver.GetResource("CheckBoxCheckBackgroundStrokeCheckedPointerOver", CheckBoxCheckBackgroundStrokeCheckedPointerOver, out CheckBoxCheckBackgroundStrokeCheckedPointerOver);
        CheckBoxCheckBackgroundStrokeCheckedPressed.GetResource("CheckBoxCheckBackgroundStrokeCheckedPressed", CheckBoxCheckBackgroundStrokeCheckedPressed, out CheckBoxCheckBackgroundStrokeCheckedPressed);
        CheckBoxCheckBackgroundStrokeCheckedDisabled.GetResource("CheckBoxCheckBackgroundStrokeCheckedDisabled", CheckBoxCheckBackgroundStrokeCheckedDisabled, out CheckBoxCheckBackgroundStrokeCheckedDisabled);
        CheckBoxCheckGlyphForegroundUnchecked.GetResource("CheckBoxCheckGlyphForegroundUnchecked", CheckBoxCheckGlyphForegroundUnchecked, out CheckBoxCheckGlyphForegroundUnchecked);
        CheckBoxCheckGlyphForegroundUncheckedPointerOver.GetResource("CheckBoxCheckGlyphForegroundUncheckedPointerOver", CheckBoxCheckGlyphForegroundUncheckedPointerOver, out CheckBoxCheckGlyphForegroundUncheckedPointerOver);
        CheckBoxCheckGlyphForegroundUncheckedPressed.GetResource("CheckBoxCheckGlyphForegroundUncheckedPressed", CheckBoxCheckGlyphForegroundUncheckedPressed, out CheckBoxCheckGlyphForegroundUncheckedPressed);
        CheckBoxCheckGlyphForegroundUncheckedDisabled.GetResource("CheckBoxCheckGlyphForegroundUncheckedDisabled", CheckBoxCheckGlyphForegroundUncheckedDisabled, out CheckBoxCheckGlyphForegroundUncheckedDisabled);
        CheckBoxCheckGlyphForegroundChecked.GetResource("CheckBoxCheckGlyphForegroundChecked", CheckBoxCheckGlyphForegroundChecked, out CheckBoxCheckGlyphForegroundChecked);
        CheckBoxCheckGlyphForegroundCheckedPointerOver.GetResource("CheckBoxCheckGlyphForegroundCheckedPointerOver", CheckBoxCheckGlyphForegroundCheckedPointerOver, out CheckBoxCheckGlyphForegroundCheckedPointerOver);
        CheckBoxCheckGlyphForegroundCheckedPressed.GetResource("CheckBoxCheckGlyphForegroundCheckedPressed", CheckBoxCheckGlyphForegroundCheckedPressed, out CheckBoxCheckGlyphForegroundCheckedPressed);
        CheckBoxCheckGlyphForegroundCheckedDisabled.GetResource("CheckBoxCheckGlyphForegroundCheckedDisabled", CheckBoxCheckGlyphForegroundCheckedDisabled, out CheckBoxCheckGlyphForegroundCheckedDisabled);
        CheckBoxCheckGlyphForegroundIndeterminate.GetResource("CheckBoxCheckGlyphForegroundIndeterminate", CheckBoxCheckGlyphForegroundIndeterminate, out CheckBoxCheckGlyphForegroundIndeterminate);
        CheckBoxCheckGlyphForegroundIndeterminatePointerOver.GetResource("CheckBoxCheckGlyphForegroundIndeterminatePointerOver", CheckBoxCheckGlyphForegroundIndeterminatePointerOver, out CheckBoxCheckGlyphForegroundIndeterminatePointerOver);
        CheckBoxCheckGlyphForegroundIndeterminatePressed.GetResource("CheckBoxCheckGlyphForegroundIndeterminatePressed", CheckBoxCheckGlyphForegroundIndeterminatePressed, out CheckBoxCheckGlyphForegroundIndeterminatePressed);
        CheckBoxCheckGlyphForegroundIndeterminateDisabled.GetResource("CheckBoxCheckGlyphForegroundIndeterminateDisabled", CheckBoxCheckGlyphForegroundIndeterminateDisabled, out CheckBoxCheckGlyphForegroundIndeterminateDisabled);

        var thickness = 1;
        var glyphThickness = 3;
        CheckBoxCheckBackgroundFillUncheckedPen = new Pen(CheckBoxCheckBackgroundFillUnchecked, thickness);
        CheckBoxCheckBackgroundFillUncheckedPointerOverPen = new Pen(CheckBoxCheckBackgroundFillUncheckedPointerOver, thickness);
        CheckBoxCheckBackgroundFillUncheckedPressedPen = new Pen(CheckBoxCheckBackgroundFillUncheckedPressed, thickness);
        CheckBoxCheckBackgroundFillUncheckedDisabledPen = new Pen(CheckBoxCheckBackgroundFillUncheckedDisabled, thickness);
        CheckBoxCheckBackgroundFillCheckedPen = new Pen(CheckBoxCheckBackgroundFillChecked, thickness);
        CheckBoxCheckBackgroundFillCheckedPointerOverPen = new Pen(CheckBoxCheckBackgroundFillCheckedPointerOver, thickness);
        CheckBoxCheckBackgroundFillCheckedPressedPen = new Pen(CheckBoxCheckBackgroundFillCheckedPressed, thickness);
        CheckBoxCheckBackgroundFillCheckedDisabledPen = new Pen(CheckBoxCheckBackgroundFillCheckedDisabled, thickness);
        CheckBoxCheckBackgroundStrokeUncheckedPen = new Pen(CheckBoxCheckBackgroundStrokeUnchecked, thickness);
        CheckBoxCheckBackgroundStrokeUncheckedPointerOverPen = new Pen(CheckBoxCheckBackgroundStrokeUncheckedPointerOver, thickness);
        CheckBoxCheckBackgroundStrokeUncheckedPressedPen = new Pen(CheckBoxCheckBackgroundStrokeUncheckedPressed, thickness);
        CheckBoxCheckBackgroundStrokeUncheckedDisabledPen = new Pen(CheckBoxCheckBackgroundStrokeUncheckedDisabled, thickness);
        CheckBoxCheckBackgroundStrokeCheckedPen = new Pen(CheckBoxCheckBackgroundStrokeChecked, thickness);
        CheckBoxCheckBackgroundStrokeCheckedPointerOverPen = new Pen(CheckBoxCheckBackgroundStrokeCheckedPointerOver, thickness);
        CheckBoxCheckBackgroundStrokeCheckedPressedPen = new Pen(CheckBoxCheckBackgroundStrokeCheckedPressed, thickness);
        CheckBoxCheckBackgroundStrokeCheckedDisabledPen = new Pen(CheckBoxCheckBackgroundStrokeCheckedDisabled, thickness);
        CheckBoxCheckGlyphForegroundUncheckedPen = new Pen(CheckBoxCheckGlyphForegroundUnchecked, glyphThickness);
        CheckBoxCheckGlyphForegroundUncheckedPointerOverPen = new Pen(CheckBoxCheckGlyphForegroundUncheckedPointerOver, glyphThickness);
        CheckBoxCheckGlyphForegroundUncheckedPressedPen = new Pen(CheckBoxCheckGlyphForegroundUncheckedPressed, glyphThickness);
        CheckBoxCheckGlyphForegroundUncheckedDisabledPen = new Pen(CheckBoxCheckGlyphForegroundUncheckedDisabled, glyphThickness);
        CheckBoxCheckGlyphForegroundCheckedPen = new Pen(CheckBoxCheckGlyphForegroundChecked, glyphThickness);
        CheckBoxCheckGlyphForegroundCheckedPointerOverPen = new Pen(CheckBoxCheckGlyphForegroundCheckedPointerOver, glyphThickness);
        CheckBoxCheckGlyphForegroundCheckedPressedPen = new Pen(CheckBoxCheckGlyphForegroundCheckedPressed, glyphThickness);
        CheckBoxCheckGlyphForegroundCheckedDisabledPen = new Pen(CheckBoxCheckGlyphForegroundCheckedDisabled, glyphThickness);
        CheckBoxCheckGlyphForegroundIndeterminatePen = new Pen(CheckBoxCheckGlyphForegroundIndeterminate, glyphThickness);
        CheckBoxCheckGlyphForegroundIndeterminatePointerOverPen = new Pen(CheckBoxCheckGlyphForegroundIndeterminatePointerOver, glyphThickness);
        CheckBoxCheckGlyphForegroundIndeterminatePressedPen = new Pen(CheckBoxCheckGlyphForegroundIndeterminatePressed, glyphThickness);
        CheckBoxCheckGlyphForegroundIndeterminateDisabledPen = new Pen(CheckBoxCheckGlyphForegroundIndeterminateDisabled, glyphThickness);
        
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
            MaxTextWidth = float.MaxValue // rect.Width // we don't want text wrapping so pass float.MaxValue
        };
        context.DrawText(ft, new Point(rect.X, rect.Center.Y - ft.Height / 2));
    }
    
    private const double CheckBoxSize = 20;
    
    public static bool IsPointInCheckBoxRect(Rect cellRect, Point mouse)
    {
        var innerRectangle = new Rect(cellRect.Center.X - CheckBoxSize / 2, cellRect.Center.Y - CheckBoxSize / 2, CheckBoxSize, CheckBoxSize);
        return innerRectangle.Contains(mouse);
    }
    
    protected void DrawCheckBox(DrawingContext context, Rect rect, bool state, int margin = 0, bool disabled = false)
    {
        rect = rect.Deflate(margin);
        const float cornerRadius = 4;
        var innerRectangle = new Rect(rect.Center.X - CheckBoxSize / 2, rect.Center.Y - CheckBoxSize / 2, CheckBoxSize, CheckBoxSize);
        bool isOver = innerRectangle.Contains(mouseCursor);
        
        var background = state ? 
            (leftPressed && isOver ? CheckBoxCheckBackgroundFillCheckedPressed : (isOver ? CheckBoxCheckBackgroundFillCheckedPointerOver : CheckBoxCheckBackgroundFillChecked)) : 
            (leftPressed && isOver ? CheckBoxCheckBackgroundFillUncheckedPressed : (isOver ? CheckBoxCheckBackgroundFillUncheckedPointerOver : CheckBoxCheckBackgroundFillUnchecked));
        
        var border = state ? 
            (leftPressed && isOver ? CheckBoxCheckBackgroundStrokeCheckedPressedPen : (isOver ? CheckBoxCheckBackgroundStrokeCheckedPointerOverPen : CheckBoxCheckBackgroundStrokeCheckedPen)) : 
            (leftPressed && isOver ? CheckBoxCheckBackgroundStrokeUncheckedPressedPen : (isOver ? CheckBoxCheckBackgroundStrokeUncheckedPointerOverPen : CheckBoxCheckBackgroundStrokeUncheckedPen));
        
        var glyph = state ?
            (leftPressed && isOver ? CheckBoxCheckGlyphForegroundCheckedPressedPen : (isOver ? CheckBoxCheckGlyphForegroundCheckedPointerOverPen : CheckBoxCheckGlyphForegroundCheckedPen)) :
            (leftPressed && isOver ? CheckBoxCheckGlyphForegroundUncheckedPressedPen : (isOver ? CheckBoxCheckGlyphForegroundUncheckedPointerOverPen : CheckBoxCheckGlyphForegroundUncheckedPen));

        if (disabled)
        {
            background = state ? CheckBoxCheckBackgroundFillCheckedDisabled : CheckBoxCheckBackgroundFillUncheckedDisabled;
            border = state ? CheckBoxCheckBackgroundStrokeCheckedDisabledPen : CheckBoxCheckBackgroundStrokeUncheckedDisabledPen;
            glyph = state ? CheckBoxCheckGlyphForegroundCheckedDisabledPen : CheckBoxCheckGlyphForegroundUncheckedDisabledPen;
        }
        
        context.DrawRectangle(background, border, innerRectangle, cornerRadius, cornerRadius);

        if (state)
        {
            var p1 = new Point(innerRectangle.Center.X - innerRectangle.Width * 0.25f, innerRectangle.Center.Y - innerRectangle.Height * 0.05f);
            var p2 = new Point(innerRectangle.Center.X - innerRectangle.Width * 0.10f, innerRectangle.Center.Y + innerRectangle.Height * 0.15f);
            var p3 = new Point(innerRectangle.Center.X + innerRectangle.Width * 0.25f, innerRectangle.Center.Y - innerRectangle.Height * 0.18f);
            context.DrawLine(glyph, p1, p2);
            context.DrawLine(glyph, p2, p3);
        }
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
                MaxTextWidth = float.MaxValue // rect.Width // we don't want text wrapping so pass float.MaxValue
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