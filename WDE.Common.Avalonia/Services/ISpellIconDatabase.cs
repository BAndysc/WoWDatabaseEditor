using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Services;

public interface ISpellIconDatabase
{
    Task<IImage?> GetIcon(uint spellId, CancellationToken cancellationToken = default);
    bool TryGetCached(uint spellId, out IImage? bitmap);
}