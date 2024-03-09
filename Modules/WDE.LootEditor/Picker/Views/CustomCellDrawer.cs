using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;
using WDE.LootEditor.Picker.ViewModels;

namespace WDE.LootEditor.Picker.Views;

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
    
    public override bool Draw(DrawingContext context, IFastTableContext table, ref Rect rect, ITableCell c, ITableRow row)
    {
        if (c is ItemViewModel.ItemNameCell itemCell &&
            itemCell.Item.MinCountOrRef >= 0)
        {
            IImage? icn = null;
            if (!itemIconService.TryGetCachedIcon(itemCell.Item.ItemOrCurrencyId, out icn))
            {
                async Task FetchAsync()
                {
                    await itemIconService.GetIcon(itemCell.Item.ItemOrCurrencyId);
                    Dispatcher.UIThread.Post(table.InvalidateVisual);
                }
                FetchAsync().ListenErrors();
            }
            if (icn != null)
                context.DrawImage(icn, new Rect(rect.X + 2, rect.Center.Y - 18 / 2, 18, 18));
            rect = rect.Deflate(new Thickness(20, 0, 0, 0));
        }
        return false;
    }
}