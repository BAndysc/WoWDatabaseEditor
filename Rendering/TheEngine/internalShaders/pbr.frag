#version 450
#include "../internalShaders/theengine.cginc"

#ifndef DEPTH_PASS
layout(location = 3) in vec4 WorldPos;
layout(location = 5) in vec3 Normal;
layout(location = 7) flat in int instanceID;
layout (location = 0) out vec4 FragColor;
#endif

// Typical metallic/roughness PBR material: albedo + metallic/roughness/AO, plus an emissive
// color*intensity term. No texture slots (the scene-view "Add Cube"/"Add Sphere" primitives are
// flat-colored placeholders) - add bindless texture indices here if textured primitives are needed.
struct MaterialData
{
    vec4 albedo;   // rgb = base color, a = opacity
    vec4 emissive; // rgb = emissive color, a = emissive intensity
    float metallic;
    float roughness;
    float ao;
    float pbr_pad1;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

#ifndef DEPTH_PASS

// Cook-Torrance microfacet BRDF (GGX distribution + Smith joint visibility + Schlick Fresnel) -
// the standard real-time metallic/roughness PBR shading model.
float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    float denom = NdotH2 * (a2 - 1.0) + 1.0;
    denom = PI * denom * denom;
    return a2 / max(denom, 1e-6);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;
    return NdotV / max(NdotV * (1.0 - k) + k, 1e-6);
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    return GeometrySchlickGGX(NdotV, roughness) * GeometrySchlickGGX(NdotL, roughness);
}

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

// Outgoing radiance contributed by one light (directional or point) via the Cook-Torrance BRDF.
vec3 PBRLight(vec3 N, vec3 V, vec3 L, vec3 radiance, vec3 albedo, vec3 F0, float metallic, float roughness)
{
    vec3 H = normalize(V + L);
    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    vec3 F = FresnelSchlick(max(dot(H, V), 0.0), F0);

    vec3 numerator = NDF * G * F;
    float denom = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 1e-4;
    vec3 specular = numerator / denom;

    // energy conservation: the fraction of light not reflected specularly is diffuse, and metals
    // have no diffuse term at all
    vec3 kD = (vec3(1.0) - F) * (1.0 - metallic);
    float NdotL = max(dot(N, L), 0.0);
    return (kD * albedo / PI + specular) * radiance * NdotL;
}

void main()
{
    PIXEL_SETUP_INSTANCING(instanceID);

    vec3 N = normalize(Normal);
    vec3 V = normalize(cameraPos.xyz - WorldPos.xyz);

    vec3 albedo = M(albedo).rgb;
    float metallic = clamp(M(metallic), 0.0, 1.0);
    // a roughness of exactly 0 makes the GGX lobe degenerate (and divides by ~0 in Smith/Fresnel),
    // so floor it just above zero for a perfect-mirror-ish but stable highlight
    float roughness = clamp(M(roughness), 0.045, 1.0);
    float ao = clamp(M(ao), 0.0, 1.0);

    uint decalPickId;
    albedo = ApplyDecals(albedo, N, WorldPos.xyz, decalPickId);

    // dielectrics get a fixed ~4% F0 (typical for non-metals); metals tint the reflectance by albedo
    vec3 F0 = mix(vec3(0.04), albedo, metallic);

    vec3 Lo = vec3(0.0);

    // primary directional light (sun), cascaded-shadowed - lightDir points FROM the light, so the
    // direction TO the light is its negation
    Lo += PBRLight(N, V, normalize(-lightDir.xyz),
        lightColor * lightIntensity * SampleCascadedShadow(WorldPos.xyz, N),
        albedo, F0, metallic, roughness);

    // secondary directional light - unshadowed, mirrors lighting()'s treatment in theengine.cginc
    Lo += PBRLight(N, V, normalize(-secondaryLightDir.xyz),
        secondaryLightColor * secondaryLightIntensity,
        albedo, F0, metallic, roughness);

    // Forward+ tiled point lights - same tile/grid lookup as lighting(), but full Cook-Torrance
    // per light instead of plain N.L diffuse, so point lights also produce specular highlights
    if (tilesX > 0 && tilesY > 0)
    {
        ivec2 tile = clamp(ivec2(gl_FragCoord.xy) / FORWARD_PLUS_TILE_SIZE, ivec2(0), ivec2(tilesX - 1, tilesY - 1));
        uint lightTileIndex = uint(tile.y * tilesX + tile.x) + uint(gridSet) * uint(FORWARD_PLUS_GRID_SLOT_TILES);
        LightGridEntry entry = lightGrid[lightTileIndex];
        for (uint i = 0u; i < entry.count; i++)
        {
            GpuPointLight light = lights[lightIndices[entry.offset + i]];

            vec3 toLight = light.positionRange.xyz - WorldPos.xyz;
            float dist = length(toLight);
            float range = light.positionRange.w;
            if (dist >= range)
                continue;

            vec3 L = toLight / max(dist, 1e-4);
            float atten = 1.0 - clamp((dist - light.params.x) / max(range - light.params.x, 1e-4), 0.0, 1.0);
            atten *= atten;
            vec3 radiance = light.color.rgb * light.color.a * atten;

            Lo += PBRLight(N, V, L, radiance, albedo, F0, metallic, roughness);
        }
    }

    // no IBL/environment map in this engine yet - approximate ambient as a flat irradiance term
    // tinted by albedo and occluded by AO (no ambient specular term to keep this honest about what
    // it's approximating)
    vec3 ambient = ambientColor.rgb * albedo * ao;
    vec3 emissive = M(emissive).rgb * M(emissive).a;

    vec3 color = ambient + Lo + emissive;
    FragColor = ApplyFog(vec4(color, M(albedo).a), WorldPos.xyz);
}

#else

void main()
{
}

#endif
