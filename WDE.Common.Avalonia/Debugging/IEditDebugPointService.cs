using System.Threading.Tasks;
using Avalonia.Controls;
using WDE.Common.Debugging;
using WDE.Module.Attributes;

namespace WDE.Common.Avalonia.Debugging;

[UniqueProvider]
public interface IEditDebugPointService
{
    Task EditDebugPointInPopup(Control owner, DebugPointId debugPointId);
    Task EditDebugPointInPopup(Control owner, DebugPointId[] debugPoints);
}