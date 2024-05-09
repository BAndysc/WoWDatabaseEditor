using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.MultiRow;

namespace WDE.DatabaseEditors.Avalonia.Views.MultiRow;

internal static class Extensions
{
    public static Rect GetThreeDotRectForCell(this Rect rect)
    {
        return new Rect(rect.Right - 32, rect.Y, 32, rect.Height);
    }
}

public class CustomCellDrawer : BaseCustomCellDrawer, ICustomCellDrawer
{
    private static IPen ModifiedCellPen = new Pen(Brushes.Orange, 1);
    private static IPen PhantomRowPen = new Pen(Brushes.CornflowerBlue, 1);
    private static IPen DuplicateRowPen = new Pen(Brushes.Red, 1);
    private static IItemIconsService itemIconService;
    private static ISpellIconDatabase spellIconService;

    static CustomCellDrawer()
    {
        ModifiedCellPen.GetResource("FastTableView.ModifiedCellPen", ModifiedCellPen, out ModifiedCellPen);
        PhantomRowPen.GetResource("FastTableView.PhantomRowPen", PhantomRowPen, out PhantomRowPen);
        itemIconService = ViewBind.ResolveViewModel<IItemIconsService>();
        spellIconService = ViewBind.ResolveViewModel<ISpellIconDatabase>();
    }

    public override void DrawRow(DrawingContext context,  IFastTableContext table, ITableRow r, Rect rect)
    {
        if (r is not DatabaseEntityViewModel row)
            return;

        if (row.Duplicate)
        {
            var pen = DuplicateRowPen;
            context.FillRectangle(pen.Brush ?? Brushes.Black, rect.Deflate(1).WithWidth(5));
            context.DrawLine(pen, rect.TopLeft,rect.TopRight);
            context.DrawLine(pen, rect.BottomLeft, rect.BottomRight);
        }
    }
    
    public override bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell c, ITableRow row)
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
                var threeDotRect = rect.GetThreeDotRectForCell();
                DrawButton(context, threeDotRect, "...", 3);
                rect = rect.Deflate(new Thickness(0, 0, threeDotRect.Width, 0));
            }
        }

        IImage? icn = null;
        bool drawIcon = false;
        if (cell.ParameterValue?.BaseParameter is IItemParameter && cell.TableField is DatabaseField<long> longField)
        {
            if (!itemIconService.TryGetCachedItemIcon((uint)longField.Current.Value, out icn))
            {
                async Task FetchAsync()
                {
                    await itemIconService.GetIcon((int)longField.Current.Value);
                    Dispatcher.UIThread.Post(table.InvalidateVisual);
                }
                FetchAsync().ListenErrors();
            }
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
                    Dispatcher.UIThread.Post(table.InvalidateVisual);
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

        if (cell.DbColumnName?.ColumnName == "text" || cell.DbColumnName?.ColumnName == "option_text")
        {
            var broadcastTextId = cell.Parent.Entity.GetCell(new ColumnFullName(null, "broadcasttextid")) as DatabaseField<long>;
            if (broadcastTextId == null)
                broadcastTextId = cell.Parent.Entity.GetCell(new ColumnFullName(null, "broadcasttext_option_text")) as DatabaseField<long>;
            if (broadcastTextId != null && cell.TableField is DatabaseField<string> textField && string.IsNullOrWhiteSpace(textField.Current.Value))
            {
                rect = rect.Deflate(new Thickness(10, 0, 0, 0));
                DrawText(context, rect, cell.ToString() ?? "");
                return true;
            }
        }

        return false;
    }
}
