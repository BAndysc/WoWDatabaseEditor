using System.Threading.Tasks;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Services;

public interface IItemIconsService
{
    Task<IImage?> GetItemIcon(uint itemId);
    bool GetCachedItemIcon(uint itemId, out IImage? image);
    
    Task<IImage?> GetCurrencyIcon(uint currencyId);
    bool GetCachedCurrencyIcon(uint currencyId, out IImage? image);

    Task<IImage?> GetIcon(int itemOrCurrencyId)
    {
        if (itemOrCurrencyId >= 0)
            return GetItemIcon((uint)itemOrCurrencyId);
        return GetCurrencyIcon((uint)-itemOrCurrencyId);
    }

    bool GetCachedIcon(int itemOrCurrencyId, out IImage? image)
    {
        if (itemOrCurrencyId >= 0)
            return GetCachedItemIcon((uint)itemOrCurrencyId, out image);
        return GetCachedCurrencyIcon((uint)-itemOrCurrencyId, out image);
    }
}