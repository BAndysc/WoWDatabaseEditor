using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaStyles;

namespace WDE.PacketViewer.Avalonia.Views
{
    public class EntryToNiceBackgroundConverter : IValueConverter
    {
        private IColorGenerator generator = new ColorsGenerator();
        private Dictionary<uint, IBrush> entryToColor = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is uint entry and > 0)
            {
                if (entryToColor.TryGetValue(entry, out var color))
                    return color;

                color = new ImmutableSolidColorBrush(generator.GetNext());
                entryToColor[entry] = color;
                return color;
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public interface IColorGenerator
    {
        Color GetNext();
        void Reset();
    }

    public class ColorsGenerator : IColorGenerator
    {
        private double hue;
        private double lumo;
        private double satu;

        private static double BaseLuminance => SystemTheme.EffectiveThemeIsDark ? 0.16 : 0.84;
        private static double LuminanceChange => SystemTheme.EffectiveThemeIsDark ? 0.1 : -0.1;

        public void Reset()
        {
            hue = 0;
            lumo = BaseLuminance;
            satu = 0.66;
        }

        public ColorsGenerator()
        {
            Reset();
        }

        public Color GetNext()
        {
            Color c = Utils.HSL2RGB(hue, satu, lumo);
            if (hue < 1)
                hue = Math.Min(1, hue + 0.1);
            else
            {
                hue = 0;
                if (LuminanceChange < 0 && lumo >= 0.5 || LuminanceChange > 0 && lumo <= 0.5)
                {
                    lumo += LuminanceChange;
                }
                else
                {
                    lumo = BaseLuminance;
                    satu -= 0.1;
                }
            }
            return c;
        }
    }
    public class Utils
    {
        public static Color HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;
            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }

            return Color.FromArgb(255, (byte)(r * 255.0f), (byte)(g * 255.0f), (byte)(b * 255.0f));
        }
    }
}