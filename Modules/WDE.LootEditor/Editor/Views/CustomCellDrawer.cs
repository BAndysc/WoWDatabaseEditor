using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaStyles.Controls.FastTableView;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Services;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Avalonia.Utils.NiceColorGenerator;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;

namespace WDE.LootEditor.Editor.Views;

public static class Extensions
{
    public static Rect GetThreeDotRectForCell(this Rect rect)
    {
        return new Rect(rect.Right - 32, rect.Y, 32, rect.Height);
    }
}

public class CustomCellDrawer : BaseCustomCellDrawer, ICustomCellDrawer
{
    private static ISolidColorBrush RelationBrush = new SolidColorBrush(Colors.Aqua);
    private static IPen DuplicateRowPen = new Pen(Brushes.Red, 1);
    private static IItemIconsService itemIconService;
    private static IColorGenerator niceColorGenerator;
    private static Dictionary<long, ISolidColorBrush> groupIdToColor;
    
    static CustomCellDrawer()
    {
        niceColorGenerator = new ColorsGenerator();
        groupIdToColor = new Dictionary<long, ISolidColorBrush>();
        itemIconService = ViewBind.ResolveViewModel<IItemIconsService>();
        RelationBrush.GetResource("TextControlSelectionHighlightColor", RelationBrush, out RelationBrush);
    }
    
    public override void DrawRow(DrawingContext context,  IFastTableContext table, ITableRow r, Rect rect)
    {
        if (r is not LootItemViewModel row)
            return;

        if (row.IsDuplicate)
        {
            var pen = DuplicateRowPen;
            context.FillRectangle(pen.Brush ?? Brushes.Black, rect.Deflate(1).WithWidth(5));
            context.DrawLine(pen, rect.TopLeft,rect.TopRight);
            context.DrawLine(pen, rect.BottomLeft, rect.BottomRight);
        }
    }
    
    private bool IsReadOnly(ITableCell cell)
    {
        if (cell is ItemNameStringCell stringCell)
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

        if (c is ItemNameStringCell itemCell && !itemCell.ViewModel.IsReference)
        {
            IImage? icn = null;
            if (!itemIconService.TryGetCachedIcon(itemCell.Item, out icn))
            {
                async Task FetchAsync()
                {
                    await itemIconService.GetIcon(itemCell.Item);
                    Dispatcher.UIThread.Post(table.InvalidateVisual);
                }
                FetchAsync().ListenErrors();
            }
            if (icn != null)
                context.DrawImage(icn, new Rect(rect.X + 2, rect.Center.Y - 18 / 2, 18, 18));
            rect = rect.Deflate(new Thickness(20, 0, 0, 0));
        }
        
        if (c is LootItemParameterCell<long> cell)
        {
            if (cell == cell.Parent.GroupId &&
                cell.Value > 0)
            {
                if (!groupIdToColor.TryGetValue(cell.Value, out var groupIdBrush))
                    groupIdToColor[cell.Value] = groupIdBrush = new SolidColorBrush(niceColorGenerator.GetNext());
                context.DrawRectangle(groupIdBrush, null, rect);
            }
            
            if (cell == cell.Parent.MinCountOrRef &&
                cell.Parent.IsReference &&
                cell.Parent.ParentVm.FocusedRow is { } focused &&
                (uint)focused.Parent.LootEntry == -cell.Value)
            {
                context.DrawRectangle(RelationBrush, null, rect);
            }
            
            if (cell.Parameter.HasItems)// && /*!cell.UseFlagsPicker*/ && !cell.UseItemPicker)
            {
                if (rect.Contains(mouseCursor) && !IsReadOnly(c))
                {
                    var threeDotRect = rect.GetThreeDotRectForCell();
                    DrawButton(context, threeDotRect, "...", 3);
                    rect = rect.Deflate(new Thickness(0, 0, threeDotRect.Width, 0));
                }
            }
            
            if (cell == cell.Parent.MinCountOrRef &&
                cell.Parent.IsReference &&
                rect.Contains(mouseCursor) &&
                !cell.Parent.ParentVm.HasLootEntry(LootSourceType.Reference, new LootEntry(cell.Parent.ReferenceEntry)))
            {
                rect = rect.Deflate(new Thickness(0, 0, 2, 0));
                var btnRect = rect.GetThreeDotRectForCell();
                DrawButton(context, btnRect, "Edit", 3);
                rect = rect.Deflate(new Thickness(0, 0, btnRect.Width, 0));
            }

        }

        if (c is LootItemParameterCell<string> stringCell)
        {
            if (stringCell.Parameter.HasItems)// && /*!cell.UseFlagsPicker*/ && !cell.UseItemPicker)
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