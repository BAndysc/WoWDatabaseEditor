#version 330 core
#include "theengine.cginc"

uniform samplerBuffer glpyhUVs;
uniform samplerBuffer glyphPositions;

out vec2 TexCoords;

void main()
{
    vec4 glpyhUV = texelFetch(glpyhUVs, gl_InstanceID);
    vec4 glyphPosition = texelFetch(glyphPositions, gl_InstanceID);

    vec2 pos = (position.xy * vec2(glyphPosition.z / screenWidth, glyphPosition.w / screenHeight) + vec2(glyphPosition.x / screenWidth, 1 - glyphPosition.y / screenHeight)) * 2 - 1;
    gl_Position = vec4(pos.x, pos.y, 0.0, 1.0);
    TexCoords = uv1 * glpyhUV.zw + glpyhUV.xy;
}