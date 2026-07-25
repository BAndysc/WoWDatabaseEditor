#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) in vec4 Color;
layout(location = 1) in vec4 TexCoord;
layout(location = 2) in vec4 WorldPos;
layout(location = 3) in vec4 SplatId;
layout(location = 4) in vec3 Normal;
layout(location = 0) out vec4 FragColor;

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    vec4 objectColor;
};

void main()
{
    float diff = max(dot(Normal, -lightDir.xyz), 0.0);
    vec3 diffuse = diff * lightColor.rgb;
    vec3 ambient = vec3(1.0, 1.0, 1.0) * 0.4;
    vec4 col = vec4(objectColor.rgb, 1.0);

    FragColor = vec4(col.rgb * (diffuse + ambient), objectColor.a);
}
