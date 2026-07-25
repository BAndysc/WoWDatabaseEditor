using TheEngine.ECS;

namespace TheEngine.Entities;

public enum LightType
{
    // Point is the default (0) on purpose: an un-initialized Light is a (zero-intensity) point
    // light, so it can never accidentally occupy one of the two directional/sun slots.
    Point = 0,
    Directional = 1,
}

/// <summary>
/// Unified light ECS component (replaces the old PointLight + standalone DirectionalLight). A
/// light's world position (point) or direction (directional, taken from the entity's LocalToWorld
/// forward) comes from its transform. <see cref="Managers.LightManager.GatherLights"/> scans every
/// Light entity each frame: point lights feed the Forward+ SSBO, the first up-to-two directional
/// lights become the primary/secondary sun (see SceneBuffer). Only the primary directional light's
/// <see cref="CastShadows"/> drives the cascaded shadow maps; CastShadows on point lights is stored
/// for the inspector but not yet implemented.
/// </summary>
public struct Light : IComponentData
{
    public LightType Type;
    public bool Disabled;
    public bool CastShadows;

    public Vector4 Color;
    public float Intensity;

    // Point-light attenuation (world units); ignored for directional lights.
    public float AttenuationStart;
    public float AttenuationEnd;

    // Directional ambient term; only the primary directional light's ambient reaches the shader.
    public Vector4 AmbientColor;
}

/// <summary>Resolved per-frame directional light (direction baked from the entity transform), built
/// by <see cref="Managers.LightManager.GatherLights"/> and consumed by the scene buffer / shadow pass.</summary>
public struct DirectionalLightData
{
    public bool Exists;
    public Vector3 Direction; // normalized; the direction the light travels
    public Vector3 Color;
    public float Intensity;
    public Vector4 AmbientColor;
    public bool CastShadows;
}
