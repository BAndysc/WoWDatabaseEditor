#version 330 core
#include "theengine.cginc"

uniform samplerBuffer glpyhUVs;
uniform samplerBuffer glyphPositions;

out vec2 TexCoords;

void main()
{
    vec4 glpyhUV = texelFetch(glpyhUVs, gl_InstanceID);
    vec4 glyphPosition = texelFetch(glyphPositions, gl_InstanceID);

    vec2 pos = (position.xy * vec2(glyphPosition.z, glyphPosition.w) + vec2(glyphPosition.x, 1 - glyphPosition.y));
    
    mat4 ModelView = view * model;
    // Column 0:
    ModelView[0][0] = 1;
    ModelView[0][1] = 0;
    ModelView[0][2] = 0;
    
    // Column 1:
    ModelView[1][0] = 0;
    ModelView[1][1] = 1;
    ModelView[1][2] = 0;
    
    // Column 2:
    ModelView[2][0] = 0;
    ModelView[2][1] = 0;
    ModelView[2][2] = 1;
    
    gl_Position = projection * ModelView * vec4(pos.x, pos.y, 0, 1.0);
    TexCoords = uv1 * glpyhUV.zw + glpyhUV.xy;
}