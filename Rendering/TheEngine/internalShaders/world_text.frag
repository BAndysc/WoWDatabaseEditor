#version 450
#extension GL_EXT_nonuniform_qualifier : require
// bindless: the font atlas arrives as a packed index in the material data (fontIndex).
layout(set = 2, binding = 0) uniform texture2D bindlessTextures[16384];
layout(set = 2, binding = 1) uniform sampler bindlessSamplers[128];
#define SAMPLE_BINDLESS(idx, uv) texture(sampler2D(bindlessTextures[nonuniformEXT(uint(idx) & 0xFFFFFu)], bindlessSamplers[nonuniformEXT(uint(idx) >> 20u)]), uv)

layout(location = 0) out vec4 FragColor;

layout(location = 0) in vec2 TexCoords;
layout(location = 1) flat in vec4 vColor;

// world text uses the shared SdfMaterialData_t UBO only for fontIndex; the fill color arrives per
// glyph through vColor (packed in drawDataX, unpacked in world_text.vert). Mode is unused here.
layout(std140, set = 1, binding = 0) uniform MaterialData
{
    vec4 fillColor;
    int mode;
    int fontIndex;
};

void main()
{
    float u_buffer = 0.25;
    float u_gamma = 0.03;
    float t = SAMPLE_BINDLESS(fontIndex, vec2(TexCoords.x, TexCoords.y)).a;

    float alpha = smoothstep(u_buffer - u_gamma, u_buffer + u_gamma, t);
    FragColor = vec4(0, 0, 0, alpha);

    u_buffer = 0.45;
    float outline = smoothstep(u_buffer - u_gamma, u_buffer + u_gamma, t);
    FragColor = mix(FragColor, vec4(1, 1, 1, 1) * vColor, outline);
}
