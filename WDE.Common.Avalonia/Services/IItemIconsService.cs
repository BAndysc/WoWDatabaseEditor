using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Services;

public interface IItemIconsService
{
    Task<IImage?> GetItemIcon(uint itemId, CancellationToken cancelToken = default);
    bool TryGetCachedItemIcon(uint itemId, out IImage? image);
    
    Task<IImage?> GetCurrencyIcon(uint currencyId, CancellationToken cancelToken = default);
    bool TryGetCachedCurrencyIcon(uint currencyId, out IImage? image);

    Task<IImage?> GetIcon(int itemOrCurrencyId, CancellationToken cancelToken = default);
    bool TryGetCachedIcon(int itemOrCurrencyId, out IImage? image);
}