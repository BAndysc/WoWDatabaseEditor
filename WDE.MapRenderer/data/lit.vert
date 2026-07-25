#version 450
#include "../internalShaders/theengine.cginc"

// Terrain per-tile vertex data now lives in STATIC set-3 global buffers (one big buffer shared by
// all loaded tiles; see ChunkManager + IStaticGlobalBuffer). Each tile's slot start offset is baked
// into its material data (heightsOffset / splatOffset), so the shader indexes [offset + local].
layout(std430, set = 3, binding = 3) readonly buffer heightsNormalBuffer { vec4 heightsNormalData[]; };
#ifndef DEPTH_PASS
layout(std430, set = 3, binding = 4) readonly buffer chunkToSplat { ivec4 chunkToSplatData[]; };
#endif

// Read the per-tile offsets from the material data array. MATERIAL_INDEX is fragment-only in the
// cginc, so resolve it here directly: each terrain tile is its own single-instance batch, so
// gl_InstanceIndex indexes the right instancing slot. Block names must match so reflection dedups
// against the fragment-stage declarations (same set-1 bindings).
// layout must match lit.frag's MaterialData (this stage only reads heightsOffset/splatOffset).
struct MaterialData { int showGrid; int heightsOffset; int splatOffset; int splatTexIndex; int holesTexIndex; int padding0; int padding1; int padding2; };
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
layout(std430, set = 1, binding = 16) readonly buffer InstancingMaterialIndex { int materialIndices[]; };
#define LIT_MATERIAL_INDEX max(materialIndices[gl_InstanceIndex], 0)

// TexCoord + ChunkId are needed by the depth pass too (terrain hole lookup); the rest is forward-only.
layout(location = 1) out vec4 TexCoord;
layout(location = 5) flat out int ChunkId;
#ifndef DEPTH_PASS
layout(location = 0) out vec4 Color;
layout(location = 2) out vec4 WorldPos;
layout(location = 3) flat out ivec4 SplatId;
layout(location = 4) out vec4 Normal;
#endif

void main()
{
    VERTEX_SETUP_INSTANCING;

    int matIdx = LIT_MATERIAL_INDEX;
    vec4 normalHeight = heightsNormalData[materials[matIdx].heightsOffset + gl_VertexIndex];
    ChunkId = gl_VertexIndex / 145;

    // the terrain height offset must be applied in every pass so the depth matches the forward pass
    vec4 worldPos = model * vec4(position.xyz + vec3(0.0, 0.0, normalHeight.w), 1.0);
    gl_Position = projection * view * worldPos;
    TexCoord = vec4(uv1, 0.0, 0.0);
#ifndef DEPTH_PASS
    SplatId = chunkToSplatData[materials[matIdx].splatOffset + gl_VertexIndex / 145];
    Normal = vec4(normalize(normalHeight.xyz), 1.0);
    WorldPos = worldPos;
    Color = color;
#endif
}
