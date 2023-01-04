using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;

namespace WDE.LootEditor.Configuration.Views;

public static class Extensions
{
    public static Rect GetThreeDotRectForCell(this Rect rect)
    {
        return new Rect(rect.Right - 32, rect.Y, 32, rect.Height);
    }
}

public class CustomCellDrawer : BaseCustomCellDrawer, ICustomCellDrawer
{
    private static IItemIconsService itemIconService;
    
    static CustomCellDrawer()
    {
        itemIconService = ViewBind.ResolveViewModel<IItemIconsService>();
    }
    
    public override void DrawRow(DrawingContext context,  IFastTableContext table, ITableRow r, Rect rect)
    {
    }
    
    private bool IsReadOnly(ITableCell cell)
    {
        if (cell is CustomLootItemSettingViewModel.ItemNameCell)
        {
            return true;
        }

        return false;
    }

    public override bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell c, ITableRow row)
    {
        if (c is ActionCell actionCell)
        {
            DrawButton(context, rect, actionCell.StringValue, 5, actionCell.Command.CanExecute(actionCell.CommandParameter));
            return true;
        }

        if (c is CustomLootItemSettingViewModel.ItemNameCell itemCell &&
            itemCell.Row.MinCountOrRef >= 0)
        {
            IImage? icn = null;
            if (!itemIconService.TryGetCachedIcon((int)itemCell.Item, out icn))
            {
                async Task FetchAsync()
                {
                    await itemIconService.GetIcon((int)itemCell.Item);
                    Dispatcher.UIThread.Post(table.InvalidateVisual);
                }
                FetchAsync().ListenErrors();
            }
            if (icn != null)
                context.DrawImage(icn, new Rect(rect.X + 2, rect.Center.Y - 18 / 2, 18, 18));
            rect = rect.Deflate(new Thickness(20, 0, 0, 0));
        }

       
        if (c is CustomLootItemSettingViewModel.TableCell<long> cell)
        {
            if (cell.Parameter.HasItems)// && /*!cell.UseFlagsPicker*/ && !cell.UseItemPicker)
            {
                if (rect.Contains(mouseCursor) && !IsReadOnly(c))
                {
                    var threeDotRect = rect.GetThreeDotRectForCell();
                    DrawButton(context, threeDotRect, "...", 3);
                    rect = rect.Deflate(new Thickness(0, 0, threeDotRect.Width, 0));
                }
            }
        }
        
        return false;
    }
}