using System;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.Managers;

namespace WDE.MapSpawns.Rendering;

/// <summary>The soft ring texture the spawn editors project under selected/pending spawns
/// (spawn groups, pools). Each module creates (and disposes) its own instance.</summary>
public static class SpawnDecalTexture
{
    public static ITexture BuildCircleTexture(IGameContext gameContext)
    {
        const int size = 256;
        var pixels = new Vector4[size * size];
        for (int y = 0; y < size; ++y)
        {
            for (int x = 0; x < size; ++x)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float dist = MathF.Sqrt(u * u + v * v);
                float inner = SmoothStep(0.55f, 0.68f, dist);
                float outer = 1f - SmoothStep(0.9f, 1f, dist);
                pixels[y * size + x] = new Vector4(1f, 1f, 1f, inner * outer);
            }
        }
        return gameContext.Engine.TextureManager.CreateTexture(pixels, size, size);
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
