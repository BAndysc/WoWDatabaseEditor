#version 330 core
#include "../internalShaders/theengine.cginc"
in vec4 Color;
in vec4 TexCoord;
in vec4 WorldPos;
in vec4 SplatId;
in vec3 Normal;
out vec4 FragColor;

uniform vec4 objectColor;

void main()
{
    float diff = max(dot(Normal, -lightDir.xyz), 0.0);
    vec3 diffuse = diff * lightColor.rgb;
    vec3 ambient = vec3(1, 1, 1) * 0.4;
    vec4 color = vec4(objectColor.rgb, 1);
        
    FragColor = vec4(color.rgb * (diffuse + ambient), 1);    
}