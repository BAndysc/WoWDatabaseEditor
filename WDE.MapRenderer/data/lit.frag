#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 1) in vec4 TexCoord;
layout(location = 5) flat in int ChunkId;
#ifndef DEPTH_PASS
layout(location = 0) in vec4 Color;
layout(location = 2) in vec4 WorldPos;
layout(location = 3) flat in ivec4 SplatId;
layout(location = 4) in vec4 Normal;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;
#endif

struct MaterialData
{
    int showGrid;
    int heightsOffset;  // set-3 heights buffer slot start (used by lit.vert)
    int splatOffset;    // set-3 chunkToSplat buffer slot start (used by lit.vert)
    int splatTexIndex;  // bindless index of the per-tile splat/alpha atlas
    int holesTexIndex;  // bindless index of the per-tile holes atlas
    int padding0;
    int padding1;
    int padding2;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

// The per-chunk splat/holes maps are merged into one per-tile atlas each (a 16x16 grid of cells,
// 16 = ChunksInBlockX; see ChunkManager). Map a chunk-local uv into this chunk's cell, clamping +
// half-texel insetting so linear filtering never reaches across a cell seam - no bleed, and the
// clamp reproduces the old per-array-layer ClampToEdge for the shadow PCF's off-edge taps.
// cellTexels = the cell size in texels (64 for splat, 4 for holes).
vec2 atlasUV(vec2 localUv, int chunkId, float cellTexels)
{
    vec2 cell = vec2(float(chunkId % 16), float(chunkId / 16));
    vec2 within = (0.5 + clamp(localUv, 0.0, 1.0) * (cellTexels - 1.0)) / cellTexels;
    return (cell + within) / 16.0;
}

const int FOG_START = 1400;
const int FOG_END = 1800;
const vec3 FOG_COLOR = vec3(1.0, 1.0, 1.0);

// SplatId carries packed bindless texture indices directly (see ChunkManager.cs), so any
// number of distinct splat textures per ADT tile is supported - no fixed palette.
vec4 sampleSplat(int idx)
{
    return SAMPLE_BINDLESS(idx, TexCoord.xy * 5.0);
}

#ifdef DEPTH_PASS

void main()
{
    // terrain holes are the only cutout; depth only, no color output
    if (SAMPLE_BINDLESS(M(holesTexIndex), atlasUV(TexCoord.xy, ChunkId, 4.0)).r > 0.5)
        discard;
    if (vMatIdx < -2000000000) // keep vMatIdx (vertex flat output) consumed
        discard;
}

#else

void main()
{
    vec4 hole = SAMPLE_BINDLESS(M(holesTexIndex), atlasUV(TexCoord.xy, ChunkId, 4.0));
    vec4 colSplat = SAMPLE_BINDLESS(M(splatTexIndex), atlasUV(TexCoord.xy, ChunkId, 64.0));

    if (hole.r > 0.5)
        discard;

    vec4 col1 = sampleSplat(SplatId.r);
    vec4 col2 = sampleSplat(SplatId.g);
    vec4 col3 = sampleSplat(SplatId.b);
    vec4 col4 = sampleSplat(SplatId.a);
    FragColor = vec4((1.0-colSplat.r-colSplat.g-colSplat.b) * col1.rgb + colSplat.r * col2.rgb + colSplat.g * col3.rgb + colSplat.b * col4.rgb, 1.0);

    float shadow = 0.0;
    for (int x = -3; x <= 3; ++x)
    {
        for (int y = -3; y <= 3; ++y)
        {
            vec4 sampled = SAMPLE_BINDLESS(M(splatTexIndex), atlasUV(vec2(TexCoord.x - x / 64.0, TexCoord.y - y / 64.0), ChunkId, 64.0));
            shadow += sampled.a;
        }
    }
    shadow /= 81.0;
    vec4 shadowed = vec4(FragColor.rgb * 0.5, 1.0);
    FragColor = mix(FragColor, shadowed, shadow * lightIntensity);

    uint decalPickId;
    FragColor = vec4(lighting(ApplyDecals(FragColor.rgb, Normal.xyz, WorldPos.xyz, decalPickId), Normal.xyz, WorldPos.xyz), 1.0);

    float dist = distance(WorldPos, cameraPos);
    float fogFactor = (clamp(dist, float(FOG_START), float(FOG_END)) - float(FOG_START)) / float(FOG_END - FOG_START);

    float isBorderOfChunk = - min(0.0, sign(fract(WorldPos.x / 533.333) * 533.333 - 0.5)) +
        - min(0.0, sign(fract(WorldPos.y / 533.333) * 533.333 - 0.5));

    FragColor = mix(FragColor, FragColor + vec4(0.4, 0.4, 0.4, 0.0), clamp(isBorderOfChunk * float(M(showGrid)), 0.0, 1.0));
    FragColor = ApplyFog(FragColor, WorldPos.xyz);

    ObjectIndexOutputBuffer = decalPickId;
}

#endif
