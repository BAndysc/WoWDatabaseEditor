using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaStyles.Utils;

namespace WDE.Common.Avalonia.Utils;

public static class ColorPickerUtils
{
        public static async Task CreateComponentBitmapAsync(
            CancellationToken cancellationToken,
            byte[] bgraPixelData,
            int width,
            int height,
            double hueOffset,
            double lightness)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        await Task.Run(() =>
        {
            Parallel.For(0, width, (x, state) =>
            {
                double h = x * 1.0 / width;
                var precomputed = ColorUtils.precompute_okhsl_to_srgb(h, 0, lightness);
                var cosH = precomputed.cosH;
                var sinH = precomputed.sinH;
                var L = precomputed.L;
                var cs = precomputed.cs;
                for (int y = 0; y < height; y++)
                {
                    var color = ColorUtils.fast_okhsl_to_srgb(x * 1.0 / width + hueOffset, 1 - y * 1.0 / height, lightness, cosH, sinH, L, cs);
                    var pixelDataIndex = y * width * 4 + x * 4;

                    // Get a new color
                    bgraPixelData[pixelDataIndex + 0] = (byte)color.Z;
                    bgraPixelData[pixelDataIndex + 1] = (byte)color.Y;
                    bgraPixelData[pixelDataIndex + 2] = (byte)color.X;
                    bgraPixelData[pixelDataIndex + 3] = 255;
                }
                if (cancellationToken.IsCancellationRequested)
                    state.Break();
            });
        });
    }
}