using TheEngine.ECS;

namespace TheEngine.Entities;

/// <summary>
/// Directional shadow configuration as an ECS component. The engine renders cascaded shadow maps
/// for the primary directional light <b>only while at least one enabled CascadeShadowMap entity
/// exists</b> - there is no global shadow toggle. Removing/disabling the component disables shadows;
/// all shadow tunables (cascade distances, resolution, biases) live here and are editable in the
/// entity inspector. See <see cref="Managers.CascadedShadowMapManager"/> and the shadow block in
/// SceneBuffer.cs / theengine.cginc.
/// </summary>
public struct CascadeShadowMap : IComponentData
{
    /// <summary>When true the entity is ignored, exactly as if the component weren't present.</summary>
    public bool Disabled;

    // Far bound of each cascade as a positive distance from the camera (view-space). Must be
    // ascending; cascade i covers [previous split (camera near for i=0), Split_i]. Split3 is the
    // overall shadow distance - geometry past it isn't shadowed and casters past it aren't collected.
    public float Split0;
    public float Split1;
    public float Split2;
    public float Split3;

    /// <summary>How far the light frustum is extruded back along the light direction so casters
    /// between the light and the cascade slice are still captured (world units).</summary>
    public float CasterExtrusion;

    /// <summary>Square depth-map resolution per cascade (texels). Changing it recreates the
    /// cascade textures, so prefer power-of-two values (1024 / 2048 / 4096).</summary>
    public int Resolution;

    /// <summary>Hardware depth bias applied while rendering the cascade depth (vkCmdSetDepthBias):
    /// constant factor and slope-scaled factor. Pushes stored depth away from the light to reduce
    /// self-shadowing acne.</summary>
    public float DepthBiasConstant;
    public float DepthBiasSlope;

    /// <summary>Shader-side sampling biases. NormalBias pushes the sample point out along the
    /// surface normal (scaled up at grazing angles) to keep acne off lit faces; ConstantBias is a
    /// flat depth-compare epsilon.</summary>
    public float NormalBias;
    public float ConstantBias;

    /// <summary>PCF blur. PcfRadius is the kernel half-size (taps = (2r+1)^2; 0 = hard shadows,
    /// higher = smoother but costlier). Blur scales the per-tap spacing in texels (wider penumbra).</summary>
    public int PcfRadius;
    public float Blur;

    /// <summary>Cross-fade band between adjacent cascades, as a fraction (0..0.5) of each cascade's
    /// range. 0 = a hard seam where the resolution changes; higher = the seam smoothly dissolves into
    /// the next cascade (costs a second PCF lookup only for fragments inside the band).</summary>
    public float CascadeBlend;

    /// <summary>Sensible default shadow settings.</summary>
    public static CascadeShadowMap CreateDefault() => new()
    {
        Disabled = false,
        Split0 = 15f,
        Split1 = 50f,
        Split2 = 150f,
        Split3 = 300f,
        CasterExtrusion = 120f,
        Resolution = 2048,
        DepthBiasConstant = 1.5f,
        DepthBiasSlope = 3.0f,
        NormalBias = 0.08f,
        ConstantBias = 0.0015f,
        PcfRadius = 1,
        Blur = 1.0f,
        CascadeBlend = 0.1f,
    };
}
