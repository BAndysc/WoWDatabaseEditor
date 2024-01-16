using System.Threading.Tasks;
using Avalonia;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Ioc;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;

namespace WDE.LootEditor.Editor.Views;

public class CustomCellInteractor : ICustomCellInteractor
{
    private IParameterPickerService ParameterPickerService => ViewBind.ContainerProvider.Resolve<IParameterPickerService>();
    private ILootService LootService => ViewBind.ContainerProvider.Resolve<ILootService>();

    private bool IsReadOnly(ITableCell cell)
    {
        if (cell is ItemNameStringCell)
        {
            return true;
        }

        return false;
    }

    public bool SpawnEditorFor(ITableRow row, string? initialText, Visual parent, Rect rect, ITableCell c)
    {
        return IsReadOnly(c);
    }

    public bool PointerDown(ITableRow row, ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton, int clickCount)
    {
        return false;
    }
    
    public bool PointerUp(ITableRow row, ITableCell c, Rect cellRect, Point mouse, bool leftButton, bool rightButton)
    {
        if (leftButton && c is ActionCell actionCell && actionCell.Command.CanExecute(actionCell.CommandParameter))
        {
            actionCell.Command.Execute(actionCell.CommandParameter);
            return true;
        }
        
        if (c is LootItemParameterCell<long> cell)
        {
            var threeDots = cellRect.GetThreeDotRectForCell();
            cellRect = cellRect.Deflate(new Thickness(0, 0, 2 + threeDots.Width, 0));
            if (leftButton && threeDots.Deflate(3).Contains(mouse) && cell.Parameter.HasItems/* && !cell.UseItemPicker && !cell.UseFlagsPicker*/)
            {
                PickLong(cell).ListenErrors();
            }
            
            if (cell == cell.Parent.MinCountOrRef &&
                cell.Parent.IsReference &&
                !cell.Parent.ParentVm.HasLootEntry(LootSourceType.Reference, new LootEntry(cell.Parent.ReferenceEntry)))
            {
                var editBtnRect = cellRect.GetThreeDotRectForCell();
                if (leftButton && editBtnRect.Deflate(3).Contains(mouse))
                {
                    EditLootReference(new LootEntry(cell.Parent.ReferenceEntry)).ListenErrors();
                }
            }
        }
        
        if (c is LootItemParameterCell<string> stringCell && !IsReadOnly(c))
        {
            var threeDots = cellRect.GetThreeDotRectForCell();
            if (leftButton && threeDots.Deflate(3).Contains(mouse) && stringCell.Parameter.HasItems/* && !cell.UseItemPicker && !cell.UseFlagsPicker*/)
            {
                PickString(stringCell).ListenErrors();
            }
        }
        
        return false;
    }

    private Task EditLootReference(LootEntry entry) => LootService.EditLoot(LootSourceType.Reference, (uint)entry, 0);

    private async Task PickLong(LootItemParameterCell<long> cell)
    {
        var (value, ok) = await ParameterPickerService.PickParameter(cell.Parameter, cell.Value);
        if (ok)
        {
            cell.UpdateFromString(value!.ToString());
        }
    }
    
    private async Task PickString(LootItemParameterCell<string> cell)
    {
        var owner = cell.Parent;
        var oldValue = cell.Value;
        var (value, ok) = await ParameterPickerService.PickParameter(cell.Parameter, oldValue);
        if (ok)
            cell.UpdateFromString(value!);
    }
}