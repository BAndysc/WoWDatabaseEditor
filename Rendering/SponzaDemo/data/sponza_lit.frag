#version 450
#include "../internalShaders/theengine.cginc"

struct MaterialData
{
    vec4 diffuseColor;
    float alphaCutoff;
    int useMask;
    int texture1Index;
    int maskTextureIndex;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

#ifdef DEPTH_PASS

// depth / shadow pass: only the cutout discard matters, no color/picking outputs (the target is
// depth-only). Consuming TexCoord + MATERIAL_INDEX keeps the vertex outputs fully matched.
layout(location = 0) in vec2 TexCoord;

void main()
{
    float alpha = M(useMask) == 1 ? SAMPLE_BINDLESS(M(maskTextureIndex), TexCoord).r : SAMPLE_BINDLESS(M(texture1Index), TexCoord).a;
    if (alpha < M(alphaCutoff))
        discard;
}

#else

layout(location = 0) in vec2 TexCoord;
layout(location = 1) in vec3 WorldNormal;
layout(location = 2) in vec3 WorldPos;
layout(location = 3) flat in int instanceID;
layout(location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;

void main()
{
    PIXEL_SETUP_INSTANCING(instanceID);
    vec4 tex = SAMPLE_BINDLESS(M(texture1Index), TexCoord);
    // foliage/chains carry their cutout in a separate mask texture (wavefront map_d)
    float alpha = M(useMask) == 1 ? SAMPLE_BINDLESS(M(maskTextureIndex), TexCoord).r : tex.a;
    if (alpha < M(alphaCutoff))
        discard;
    uint decalPickId;
    vec3 albedo = ApplyDecals(tex.rgb * M(diffuseColor).rgb, normalize(WorldNormal), WorldPos, decalPickId);
    FragColor = vec4(lighting(albedo, normalize(WorldNormal), WorldPos), 1.0);
    ObjectIndexOutputBuffer = decalPickId != 0u ? decalPickId : objectIndex;
}

#endif
