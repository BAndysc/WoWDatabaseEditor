using System.Runtime.InteropServices;
using TheMaths;

namespace TheEngine.Structures
{
    /// <summary>
    /// std430 layout for the per-frame Light SSBO (set 0, binding <see cref="Constants.LIGHT_BUFFER_BINDING"/>).
    /// Mirrors the GpuPointLight struct declared in theengine.cginc.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GpuPointLight
    {
        // xyz = world position, w = attenuation end (range)
        public Vector4 PositionRange;
        // rgb = color, a = intensity
        public Vector4 Color;
        // x = attenuation start
        public Vector4 Params;
    }
}
