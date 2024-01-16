using System.Threading.Tasks;
using Avalonia;
using AvaloniaStyles.Controls.FastTableView;
using Prism.Ioc;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.LootEditor.Editor.ViewModels;

namespace WDE.LootEditor.Configuration.Views;

public class CustomCellInteractor : ICustomCellInteractor
{
    private IParameterPickerService ParameterPickerService => ViewBind.ContainerProvider.Resolve<IParameterPickerService>();

    private bool IsReadOnly(ITableCell cell)
    {
        if (cell is CustomLootItemSettingViewModel.ItemNameCell)
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
        
        if (c is CustomLootItemSettingViewModel.TableCell<long> cell)
        {
            var threeDots = Editor.Views.Extensions.GetThreeDotRectForCell(cellRect);
            if (leftButton && threeDots.Deflate(3).Contains(mouse) && cell.Parameter.HasItems/* && !cell.UseItemPicker && !cell.UseFlagsPicker*/)
            {
                PickLong(cell).ListenErrors();
            }
        }
        
        return false;
    }
    
    private async Task PickLong(CustomLootItemSettingViewModel.TableCell<long> cell)
    {
        var (value, ok) = await ParameterPickerService.PickParameter(cell.Parameter, cell.Value);
        if (ok)
        {
            cell.UpdateFromString(value!.ToString());
        }
    }
}