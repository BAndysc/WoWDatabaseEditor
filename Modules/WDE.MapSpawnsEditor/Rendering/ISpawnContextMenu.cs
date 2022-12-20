using System.Windows.Input;
using WDE.Module.Attributes;

namespace WDE.MapSpawnsEditor.Rendering;

[UniqueProvider]
public interface ISpawnContextMenu
{
    IEnumerable<(string, ICommand, object?)>? GenerateContextMenu();
}