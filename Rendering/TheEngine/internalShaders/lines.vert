#version 450
#include "theengine.cginc"

// vertex pulling: no vertex attributes are read, two vec4 per vertex
// [gl_VertexIndex * 2] = world position, [gl_VertexIndex * 2 + 1] = color
layout(std430, set = 1, binding = 1) readonly buffer LineVertices { vec4 lineVerts[]; };

layout(location = 0) out vec4 lineColor;

void main()
{
    vec4 worldPos = lineVerts[gl_VertexIndex * 2];
    lineColor = lineVerts[gl_VertexIndex * 2 + 1];
    gl_Position = projection * view * vec4(worldPos.xyz, 1.0);
}
