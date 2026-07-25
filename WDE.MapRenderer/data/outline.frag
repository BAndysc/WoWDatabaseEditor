#version 450
#extension GL_EXT_nonuniform_qualifier : require
// bindless: the outline/unblurred/scene textures arrive as packed indices in the material data.
layout(set = 2, binding = 0) uniform texture2D bindlessTextures[16384];
layout(set = 2, binding = 1) uniform sampler bindlessSamplers[128];
#define SAMPLE_BINDLESS(idx, uv) texture(sampler2D(bindlessTextures[nonuniformEXT(uint(idx) & 0xFFFFFu)], bindlessSamplers[nonuniformEXT(uint(idx) >> 20u)]), uv)
#define SAMPLE_BINDLESS_LOD(idx, uv, lod) textureLod(sampler2D(bindlessTextures[nonuniformEXT(uint(idx) & 0xFFFFFu)], bindlessSamplers[nonuniformEXT(uint(idx) >> 20u)]), uv, lod)

layout(location = 0) out vec4 FragColor;

layout(location = 0) in vec2 TexCoords;

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    vec4 outlineColor;
    int outlineTexIndex;
    int outlineTexUnBlurredIndex;
    int mainTexIndex;
    int padding0;
};

void main()
{
    vec4 tex = SAMPLE_BINDLESS(mainTexIndex, vec2(TexCoords.x, TexCoords.y));
    vec4 outline = SAMPLE_BINDLESS_LOD(outlineTexIndex, vec2(TexCoords.x, TexCoords.y), 0.0);

    for (int x = -2; x <= 2; ++x)
    {
        for (int y = -2; y <= 2; ++y)
        {
            outline += SAMPLE_BINDLESS_LOD(outlineTexIndex, vec2(TexCoords.x, TexCoords.y) + vec2(x, y) * 0.003, 0.0);
        }
    }
    outline = outline / (1+5 * 5);

    vec4 outlineNoBlur = SAMPLE_BINDLESS_LOD(outlineTexUnBlurredIndex, vec2(TexCoords.x, TexCoords.y), 0.0);

    if (outlineNoBlur.w == 1)
    {
        FragColor = tex;
    }
    else
    {
        float line = smoothstep(0.0, 0.5, outline.w);
        FragColor = vec4(outlineColor.xyz, 1.0) * line + tex * (1.0-line);
    }
}
