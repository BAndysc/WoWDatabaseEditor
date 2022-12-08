using Avalonia;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Avalonia.Extensions;

public static class PositionSizeExtensions
{
    public static Rect ToRect(this PositionSize pos)
    {
        return new Rect(pos.X, pos.Y, pos.Width, pos.Height);
    }
    
    public static PositionSize ToPositionSize(this Rect rect)
    {
        return new PositionSize(rect.X, rect.Y, rect.Width, rect.Height);
    }
}