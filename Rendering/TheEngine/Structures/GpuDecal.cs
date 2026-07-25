using System.Runtime.InteropServices;
using TheMaths;

namespace TheEngine.Structures
{
    /// <summary>
    /// std430 layout for the per-frame Decal SSBO (set 0, binding <see cref="Constants.DECAL_BUFFER_BINDING"/>).
    /// Mirrors the GpuDecal struct declared in theengine.cginc.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GpuDecal
    {
        public Matrix WorldToLocal;    // inverse model matrix; inside-box == all |coord| <= 1
        public Vector4 PositionRadius; // xyz = world pos, w = bounding-sphere radius (cheap tile pre-test)
        public Vector4 Color;          // rgb tint, a = opacity
        public int AlbedoTextureIndex; // bindless index, resolved fresh every frame from Decal.Albedo
        public float FadeAngleCos;
        public float SortBias;         // reserved, 0 in v1
        public uint PickId;            // DecalManager.PickIdBase + frame-local decal slot, see RenderManager.PickObject
    }
}
