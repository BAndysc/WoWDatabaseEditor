#version 330 core
#include "../internalShaders/theengine.cginc"

out vec4 Color;
out vec2 TexCoord;
out vec2 TexCoord2;
out vec4 WorldPos;
out vec4 SplatId;
out vec3 Normal;
flat out int instanceID;

void main()
{
    VERTEX_SETUP_INSTANCING;
    instanceID = gl_InstanceID;

    WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
    Color = color;
    TexCoord = uv1;
    TexCoord2 = uv2;
    Normal = mat3(transpose(inverseModel)) * normalize(normal.xyz);
}