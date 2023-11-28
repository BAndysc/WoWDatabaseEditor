namespace WDE.Common.Utils;

public readonly struct RgbColor
{
    public readonly byte R;
    public readonly byte G;
    public readonly byte B;

    public RgbColor(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }
}