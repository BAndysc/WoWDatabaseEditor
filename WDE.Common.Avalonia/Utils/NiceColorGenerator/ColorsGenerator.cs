using System;
using Avalonia.Media;
using AvaloniaStyles;

namespace WDE.Common.Avalonia.Utils.NiceColorGenerator
{
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
}