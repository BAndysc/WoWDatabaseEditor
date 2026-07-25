#version 450
#extension GL_EXT_nonuniform_qualifier : require
// bindless: the source texture arrives as a packed index in the material data (see GetBindlessIndex),
// so set 1 holds only the MaterialData UBO and never a per-draw sampler.
layout(set = 2, binding = 0) uniform texture2D bindlessTextures[16384];
layout(set = 2, binding = 1) uniform sampler bindlessSamplers[128];
#define SAMPLE_BINDLESS(idx, uv) texture(sampler2D(bindlessTextures[nonuniformEXT(uint(idx) & 0xFFFFFu)], bindlessSamplers[nonuniformEXT(uint(idx) >> 20u)]), uv)

layout(location = 0) out vec4 FragColor;

layout(location = 0) in vec2 TexCoords;

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    int flipY;
    int texture1Index;
    int padding2;
    int padding3;
};

void main()
{
    FragColor = SAMPLE_BINDLESS(texture1Index, vec2(TexCoords.x, mix(TexCoords.y, 1.0 - TexCoords.y, float(flipY))));
}
