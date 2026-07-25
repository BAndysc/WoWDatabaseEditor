#version 450
#include "theengine.cginc"

layout(std430, set = 1, binding = 1) readonly buffer glpyhUVs { vec4 glyphUvArr[]; };
layout(std430, set = 1, binding = 2) readonly buffer glyphPositions { vec4 glyphPosArr[]; };

layout(location = 0) out vec2 TexCoords;

void main()
{
    vec4 glpyhUV = glyphUvArr[gl_InstanceIndex];
    vec4 glyphPosition = glyphPosArr[gl_InstanceIndex];

    vec2 pos = (position.xy * vec2(glyphPosition.z / screenWidth, glyphPosition.w / screenHeight) + vec2(glyphPosition.x / screenWidth, 1.0 - glyphPosition.y / screenHeight)) * 2.0 - 1.0;
    gl_Position = vec4(pos.x, pos.y, 0.0, 1.0);
    TexCoords = uv1 * glpyhUV.zw + glpyhUV.xy;
}
