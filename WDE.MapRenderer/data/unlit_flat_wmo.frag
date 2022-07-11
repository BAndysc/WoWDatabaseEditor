#version 330 core
#include "../internalShaders/theengine.cginc"
in vec4 Color;
in vec2 TexCoord;
in vec2 TexCoord2;
in vec4 WorldPos;
in vec4 SplatId;
in vec3 Normal;
flat in int instanceID;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;

uniform sampler2D texture1;
uniform vec4 mesh_color;
uniform float alphaTest;

void main()
{
    vec4 tex1 = texture(texture1, TexCoord.xy);

    if (tex1.a < alphaTest) 
        discard;
    
    FragColor = mesh_color;
}