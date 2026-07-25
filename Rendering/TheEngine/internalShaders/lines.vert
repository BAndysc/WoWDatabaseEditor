#version 330 core
#include "theengine.cginc"

// vertex pulling: no vertex attributes, two float4 texels per vertex
// [gl_VertexID * 2] = world position, [gl_VertexID * 2 + 1] = color
uniform samplerBuffer LineVertices;

out vec4 lineColor;

void main()
{
    vec4 worldPos = texelFetch(LineVertices, gl_VertexID * 2);
    lineColor = texelFetch(LineVertices, gl_VertexID * 2 + 1);
    gl_Position = projection * view * vec4(worldPos.xyz, 1.0);
}
