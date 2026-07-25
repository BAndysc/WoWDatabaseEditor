using System.Runtime.InteropServices;
using TheMaths;

namespace TheEngine.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SceneBuffer
    {
        public Matrix ViewMatrix;
        public Matrix ProjectionMatrix;
        public Matrix ViewMatrixInverse;
        public Matrix ProjectionMatrixInverse;
        public Vector4 CameraPosition;
        public Vector4 LightDirection;
        
        public Vector3 LightColor;
        public float LightIntensity;
        
        public Vector4 AmbientColor;
        
        public Vector3 LightPosition;
        public float Align0;
        
        public Vector4 SecondaryLightDirection;
        
        public Vector3 SecondaryLightColor;
        public float SecondaryLightIntensity;

        public float fogStart;
        public float fogEnd;
        public float fogEnabled;
        public float Align1;
        public Vector4 fogColor;
        
        public float ScreenWidth;
        public float ScreenHeight;
        public float Time;
        public float ZNear;

        public float ZFar;
        public int LightCount;
        public int TilesX;
        public int TilesY;
        public int DecalCount;
        // 0 = main view's Light/DecalGrid+IndexList, 1 = the scene view's own independent set -
        // see Constants.SCENE_*_BINDING and theengine.cginc's lighting()/ApplyDecals().
        public int GridSet;

        // std140 aligns the following mat4 array to a 16-byte boundary. GridSet ends at byte 440,
        // so 8 bytes of explicit padding push CascadeViewProj0 to 448, matching the shader's std140
        // layout. WITHOUT this the shader reads shifted garbage matrices and shadows break. Mirrors
        // gridSetPad0/1 in theengine.cginc's SceneData block.
        public int GridSetPad0;
        public int GridSetPad1;

        // Cascaded shadow maps - mirrors theengine.cginc's SceneData block (must match field order/layout).
        // The four matrices map to `mat4 cascadeViewProj[NUM_CASCADES]` (std140 stride 64 == sizeof(Matrix)),
        // so they MUST stay contiguous and in order. CascadeCount == 0 disables shadow sampling.
        public Matrix CascadeViewProj0;
        public Matrix CascadeViewProj1;
        public Matrix CascadeViewProj2;
        public Matrix CascadeViewProj3;
        public Vector4 CascadeSplits;
        public int CascadeTexture0;
        public int CascadeTexture1;
        public int CascadeTexture2;
        public int CascadeTexture3;
        public int CascadeCount;
        public float ShadowMapResolution;
        public float ShadowNormalBias;
        public float ShadowConstantBias;
        // PCF blur + cascade-blend controls (own std140 16-byte block, padded). ShadowPcfRadius =
        // kernel half-size, ShadowBlur = per-tap texel spacing, ShadowCascadeBlend = cross-fade band
        // fraction. Mirrors theengine.cginc's SceneData block.
        public int ShadowPcfRadius;
        public float ShadowBlur;
        public float ShadowCascadeBlend;
        public int ShadowPad1;
    }
}
