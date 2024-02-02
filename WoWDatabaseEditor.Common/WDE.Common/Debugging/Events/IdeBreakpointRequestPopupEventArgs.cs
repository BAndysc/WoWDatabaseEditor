namespace WDE.Common.Debugging;

public class IdeBreakpointRequestPopupEventArgs
{
    public DebugPointId HitDebugPoint { get; }

    public IdeBreakpointRequestPopupEventArgs(DebugPointId hitDebugPoint)
    {
        HitDebugPoint = hitDebugPoint;
    }

    // mutable
    public bool Handled { get; set; }
    public object? AttachPopupToObject { get; set; }
    public double PopupOffsetX { get; set; }
    public double PopupOffsetY { get; set; }
}