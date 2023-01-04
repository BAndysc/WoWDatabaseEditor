using System;
using Avalonia.Media;
using AvaloniaStyles;
using AvaloniaStyles.Utils;
using HslColor = AvaloniaStyles.Utils.HslColor;

namespace WDE.Common.Avalonia.Utils.NiceColorGenerator
{
    public class ColorsGenerator : IColorGenerator
    {
        private double hue;
        private double lumo;
        private double satu;

        private double baseSaturation;
        private double? customLuminance;
        private double BaseLuminance => customLuminance ?? (SystemTheme.EffectiveThemeIsDark ? 0.16 : 0.84);
        private double LuminanceChange => SystemTheme.EffectiveThemeIsDark ? 0.05 : -0.05;

        public void Reset()
        {
            hue = 0;
            lumo = BaseLuminance;
            satu = baseSaturation;
        }

        public ColorsGenerator(double customSaturation = 0.66, double? customLuminance = null)
        {
            baseSaturation = customSaturation;
            this.customLuminance = customLuminance;
            Reset();
        }

        public Color GetNext()
        {
            Color c = new HslColor(hue, satu, lumo).ToRgba();
            if (hue < 0.92)
                hue = Math.Min(1, hue + 0.08);
            else
            {
                hue = 0;
                if (LuminanceChange < 0 && lumo >= 0.65 || LuminanceChange > 0 && lumo <= 0.35)
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
}