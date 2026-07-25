#version 450
#include "theengine.cginc"

layout(location = 0) in vec2 TexCoords;
layout(location = 0) out vec4 FragColor;

// mode mirrors TheEngine.Rendering.DebugView; depthIndex is the bindless slot of the view's depth.
layout(std140, set = 1, binding = 0) uniform MaterialData
{
    int mode;
    int depthIndex;
    int debugPad2;
    int debugPad3;
};

// 0..1 -> blue -> cyan -> green -> yellow -> red
vec3 heat(float t)
{
    t = clamp(t, 0.0, 1.0);
    return clamp(vec3(t * 2.0 - 1.0, 1.0 - abs(t * 2.0 - 1.0), 1.0 - t * 2.0), 0.0, 1.0);
}

void main()
{
    if (mode == 1) // depth prepass, linearized to [0,1]
    {
        float lin = Linear01Depth(SAMPLE_BINDLESS(depthIndex, TexCoords).r);
        FragColor = vec4(vec3(lin), 1.0);
    }
    else if (mode == 2) // all shadow cascade depth maps, tiled 2x2 (cascade 0 = top-left)
    {
        // pick the cell, then the local uv inside it. cascade index: col + row*2, so
        // 0=TL 1=TR 2=BL 3=BR. row 0 is the top because targets are stored top-down (v=0 == top).
        ivec2 cell = clamp(ivec2(TexCoords * 2.0), ivec2(0), ivec2(1));
        int cascade = cell.x + cell.y * 2;
        vec2 cellUV = fract(TexCoords * 2.0);
        float d = (cascadeCount > cascade) ? SAMPLE_BINDLESS(cascadeTextureIndices[cascade], cellUV).r : 1.0;
        // thin separators between the four tiles so the cascade boundaries are visible
        vec2 g = abs(cellUV - 0.5);
        float border = (max(g.x, g.y) > 0.497) ? 1.0 : 0.0;
        FragColor = vec4(mix(vec3(d), vec3(0.15, 0.6, 0.15), border), 1.0);
    }
    else if (mode == 3 || mode == 4) // Forward+ light / decal per-tile complexity heatmap
    {
        if (tilesX <= 0 || tilesY <= 0)
        {
            FragColor = vec4(0.0, 0.0, 0.0, 1.0);
            return;
        }
        ivec2 tile = clamp(ivec2(TexCoords * vec2(tilesX, tilesY)), ivec2(0), ivec2(tilesX - 1, tilesY - 1));
        uint gridIdx = uint(tile.y * tilesX + tile.x) + uint(gridSet) * uint(FORWARD_PLUS_GRID_SLOT_TILES);
        uint count = mode == 3 ? lightGrid[gridIdx].count : decalGrid[gridIdx].count;
        float maxPerTile = mode == 3 ? float(FORWARD_PLUS_MAX_LIGHTS_PER_TILE) : float(FORWARD_PLUS_MAX_DECALS_PER_TILE);
        FragColor = vec4(count > 0u ? heat(float(count) / maxPerTile) : vec3(0.0), 1.0);
    }
    else
    {
        FragColor = vec4(1.0, 0.0, 1.0, 1.0);
    }
}
