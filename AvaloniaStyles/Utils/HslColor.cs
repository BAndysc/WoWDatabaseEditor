using System;
using Avalonia.Media;

namespace AvaloniaStyles.Utils;

public class HslDiff
{
    public HslDiff(double h, double s, double l)
    {
        H = h;
        S = s;
        L = l;
    }

    public HslDiff() {}
    
    public double H { get; set; }
    public double S { get; set; }
    public double L { get; set; }
}

public readonly struct HslColor
{
    public readonly double H;
    public readonly double S;
    public readonly double L;

    public double V => L;
    
    public HslColor(double h, double s, double v)
    {
        H = h;
        S = s;
        L = v;
    }

    // to hsl
    public static HslColor FromRgba(Color rgba)
    {
        var result = ColorUtils.srgb_to_okhsl(rgba.R, rgba.G, rgba.B);
        return new HslColor(result.X, result.Y, result.Z);
    }

    public Color ToRgba(double opacity = 1)
    {
        var result = ColorUtils.okhsl_to_srgb(H, S, L);
        return new Color((byte)(opacity * 255), (byte)result.X, (byte)result.Y, (byte)result.Z);
    }

    public HslColor ClampL(double min, double max)
    {
        return new HslColor(H, S, Math.Clamp(L, min, max));
    }

    public HslColor Scale(HslDiff? scaler, double minL, double maxL)
    {
        if (scaler == null)
            return this;

        double l = L;
        var scalerL = Math.Clamp(scaler.L, minL, maxL);
        if (scalerL > 0.5)
        {
            var intensity01 = 1 - (Math.Clamp(scalerL, 0, 1) - 0.5) * 2;
            var slower = Math.Pow(intensity01, 0.1);
            l = slower * Math.Pow(L, intensity01) + 1 - slower;
        }
        else if (scalerL < 0.5)
        {
            var intensity01 = Math.Clamp(scalerL, 0, 1) * 2;
            var slower = Math.Sqrt(intensity01);
            l = slower * Math.Pow(L, 1 / intensity01);
        }
        return new HslColor(scaler.H, Math.Clamp(S * (scaler.S * 2), 0, 1), l);
    }
    
    public HslColor WithHue(double hue)
    {
        return new HslColor(hue, S, L);
    }

    public HslColor WithLightness(double lightness)
    {
        return new HslColor(H, S, Math.Clamp(lightness, 0, 1));
    }
}