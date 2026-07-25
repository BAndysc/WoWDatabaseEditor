using System.Runtime.InteropServices;

namespace TheEngine.Structures
{
    /// <summary>
    /// Push constants for internalShaders/tile_cull.comp (compute stage only), shared by the
    /// merged light+decal culling dispatch. Mirrors the GLSL push_constant block declared there.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct LightCullPushConstants
    {
        // packed bindless index of the depth prepass texture (see GetBindlessIndex)
        public int DepthTextureIndex;
        // dynamic-resolution render size in pixels (DynamicWidth/DynamicHeight)
        public int ScreenWidth;
        public int ScreenHeight;
        // 0 = write the main view's grid/index SSBOs, 1 = write the scene view's own independent
        // set (see Constants.SCENE_*_BINDING)
        public int IsSceneView;
    }
}
