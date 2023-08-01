using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Avalonia.Services;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.Avalonia.Views.MultiRow;

public abstract class CustomCellDrawerInteractorBase
{
    protected Rect GetThreeDotRectForCell(Rect rect)
    {
        return new Rect(rect.Right - 32, rect.Y, 32, rect.Height);
    }
}

public class CustomCellDrawer : CustomCellDrawerInteractorBase, ICustomCellDrawer
{
    private static IPen ModifiedCellPen = new Pen(Brushes.Orange, 1);
    private static IPen PhantomRowPen = new Pen(Brushes.CornflowerBlue, 1);
    private static IPen DuplicateRowPen = new Pen(Brushes.Red, 1);
    private static IPen ButtonTextPen = new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 1);
    private static IPen ButtonBorderPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 245, 245)), 1);
    private static IPen ButtonBackgroundPen = new Pen(new SolidColorBrush(Colors.White), 0);
    private static IPen ButtonBackgroundHoverPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 245, 245)), 0);
    private static IPen ButtonBackgroundPressedPen = new Pen(new SolidColorBrush(Color.FromRgb(215, 215, 215)), 0);
    private static IPen ButtonBackgroundDisabledPen = new Pen(new SolidColorBrush(Color.FromRgb(170, 170, 170)), 0);
    private static ISolidColorBrush TextBrush = new SolidColorBrush(Color.FromRgb(41, 41, 41));
    private static ISolidColorBrush FocusTextBrush = new SolidColorBrush(Colors.White);
    private static IItemIconsService itemIconService;
    private static ISpellIconDatabase spellIconService;

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
        TextBrush.GetResource("FastTableView.TextBrush", TextBrush, out TextBrush);
        FocusTextBrush.GetResource("FastTableView.FocusTextBrush", FocusTextBrush, out FocusTextBrush);
        itemIconService = ViewBind.ResolveViewModel<IItemIconsService>();
        spellIconService = ViewBind.ResolveViewModel<ISpellIconDatabase>();
    }

    public void DrawRow(DrawingContext context,  IFastTableContext table, ITableRow r, Rect rect)
    {
        if (r is not DatabaseEntityViewModel row)
            return;

        if (row.Duplicate)
        {
            var pen = DuplicateRowPen;
            context.FillRectangle(pen.Brush, rect.Deflate(1).WithWidth(5));
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

    public bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell c)
    {
        if (c is not DatabaseCellViewModel cell)
            return false;

        if (cell.ActionCommand != null)
        {
            DrawButton(context, rect, cell.ActionLabel, 3, cell.ActionCommand.CanExecute(null));
            return true;
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

        IImage? icn = null;
        bool drawIcon = false;
        if (cell.ParameterValue?.BaseParameter is IItemParameter && cell.TableField is DatabaseField<long> longField)
        {
            icn = itemIconService.GetIcon((uint)longField.Current.Value);
            drawIcon = true;
        }
        else if (cell.ParameterValue?.BaseParameter is ISpellParameter && cell.TableField is DatabaseField<long> longSpellField)
        {
            var spellId = (uint)longSpellField.Current.Value;
            if (!spellIconService.TryGetCached(spellId, out icn))
            {
                async Task UpdateIcon(uint spell)
                {
                    await spellIconService.GetIcon(spell);
                    table.InvalidateVisual();
                }
                UpdateIcon(spellId).ListenErrors();
            }
            drawIcon = true;
        }

        if (drawIcon)
        {
            if (icn != null)
            {
                context.DrawImage(icn, new Rect(rect.X + 2, rect.Center.Y - 18 / 2, 18, 18));
            }
            rect = rect.Deflate(new Thickness(20, 0, 0, 0));
        }

        if (cell.DbColumnName == "text" || cell.DbColumnName == "option_text")
        {
            var broadcastTextId = cell.Parent.Entity.GetCell("broadcasttextid") as DatabaseField<long>;
            if (broadcastTextId == null)
                broadcastTextId = cell.Parent.Entity.GetCell("broadcasttext_option_text") as DatabaseField<long>;
            if (broadcastTextId != null && cell.TableField is DatabaseField<string> textField && string.IsNullOrWhiteSpace(textField.Current.Value))
            {
                rect = rect.Deflate(new Thickness(10, 0, 0, 0));
                var ft = new FormattedText
                {
                    Text = cell.ToString(),
                    Constraint = new Size(rect.Width, rect.Height),
                    Typeface = Typeface.Default,
                    FontSize = 12
                };
                var textColor = new SolidColorBrush(TextBrush.Color, 0.4f);
                context.DrawText(textColor, new Point(rect.X, rect.Center.Y - ft.Bounds.Height / 2), ft);
                return true;
            }
        }

        return false;
    }
}