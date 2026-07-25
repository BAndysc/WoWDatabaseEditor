#version 450
#include "theengine.cginc"

layout(std430, set = 1, binding = 1) readonly buffer glpyhUVs { vec4 glyphUvArr[]; };
layout(std430, set = 1, binding = 2) readonly buffer glyphPositions { vec4 glyphPosArr[]; };

layout(location = 0) out vec2 TexCoords;
layout(location = 1) flat out vec4 vColor;

void main()
{
    VERTEX_SETUP_INSTANCING;
    vec4 glpyhUV = glyphUvArr[gl_InstanceIndex];
    vec4 glyphPosition = glyphPosArr[gl_InstanceIndex];

    // per-glyph color is packed RGBA8 in drawDataX so all labels can share one batched draw
    // (no per-label MaterialData UBO, which would otherwise force a fresh set=1 per label).
    uint packed = uint(drawDataX);
    vColor = vec4(packed & 0xFFu, (packed >> 8) & 0xFFu, (packed >> 16) & 0xFFu, (packed >> 24) & 0xFFu) / 255.0;

    vec2 pos = (position.xy * vec2(glyphPosition.z, glyphPosition.w) + vec2(glyphPosition.x, 1.0 - glyphPosition.y));

    mat4 ModelView = view * model;
    // Column 0:
    ModelView[0][0] = 1.0;
    ModelView[0][1] = 0.0;
    ModelView[0][2] = 0.0;

    // Column 1:
    ModelView[1][0] = 0.0;
    ModelView[1][1] = 1.0;
    ModelView[1][2] = 0.0;

    // Column 2:
    ModelView[2][0] = 0.0;
    ModelView[2][1] = 0.0;
    ModelView[2][2] = 1.0;

    gl_Position = projection * ModelView * vec4(pos.x, pos.y, 0, 1.0);
    TexCoords = uv1 * glpyhUV.zw + glpyhUV.xy;
}
