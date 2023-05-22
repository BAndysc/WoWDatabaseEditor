using Avalonia.Media;

namespace WDE.Common.Avalonia.Services;

public interface IItemIconsService
{
    IImage? GetIcon(string name);
    IImage? GetIcon(uint itemId);
}