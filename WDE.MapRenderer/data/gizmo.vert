#version 330 core
#include "../internalShaders/theengine.cginc"

out vec4 Color;
out vec4 TexCoord;
out vec4 WorldPos;
out vec4 SplatId;
out vec3 Normal;

void main()
{
    WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
    Color = color;
    TexCoord = vec4(uv1, 0, 0);
    Normal = normalize(mat3(transpose(inverseModel)) * normalize(normal.xyz));
}