using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using WDE.Common.Avalonia.Utils;

namespace WDE.Common.Avalonia.Services;

public interface ISpellIconDatabase
{
    static ISpellIconDatabase Instance { get; } = ViewBind.ResolveViewModel<SpellIconDatabase>();
    Task<Bitmap?> GetIcon(uint spellId, CancellationToken cancellationToken = default);
    bool TryGetCached(uint spellId, out Bitmap? bitmap);
}