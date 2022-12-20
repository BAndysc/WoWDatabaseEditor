namespace WDE.SmartScriptEditor.Models;

public readonly struct PositionSize
{
    public readonly double X;
    public readonly double Y;
    public readonly double Width;
    public readonly double Height;

    public PositionSize(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double Bottom => Y + Height;
}