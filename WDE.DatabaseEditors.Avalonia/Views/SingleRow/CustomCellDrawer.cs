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
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels.SingleRow;

namespace WDE.DatabaseEditors.Avalonia.Views.SingleRow;

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
    private static IItemIconsService itemIconService;
    private static ISpellIconDatabase spellIconService;
    
    static CustomCellDrawer()
    {
        ModifiedCellPen.GetResource("FastTableView.ModifiedCellPen", ModifiedCellPen, out ModifiedCellPen);
        PhantomRowPen.GetResource("FastTableView.PhantomRowPen", PhantomRowPen, out PhantomRowPen);
        itemIconService = ViewBind.ResolveViewModel<IItemIconsService>();
        spellIconService = ViewBind.ResolveViewModel<ISpellIconDatabase>();
    }

    public override void DrawRow(DrawingContext context, IFastTableContext table, ITableRow r, Rect rect)
    {
        if (r is not DatabaseEntityViewModel row)
            return;

        if (!row.Entity.ExistInDatabase)
        {
            var pen = row.IsPhantomEntity ? PhantomRowPen : ModifiedCellPen;
            context.FillRectangle(pen.Brush ?? Brushes.Black, rect.WithWidth(5));
            context.DrawLine(pen, rect.TopLeft,rect.TopRight);
            context.DrawLine(pen, rect.BottomLeft, rect.BottomRight);
        }
    }

    public override bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell c, ITableRow row)
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
            context.DrawRectangle(null, ModifiedCellPen, rect.Deflate(1));
            // don't return true, because we want to draw original value anyway
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
                    table.InvalidateVisual();
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

        return false;
    }
}
