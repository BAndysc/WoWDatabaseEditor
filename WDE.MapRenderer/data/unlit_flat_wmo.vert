#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec4 Color;
layout(location = 1) out vec2 TexCoord;
layout(location = 2) out vec2 TexCoord2;
layout(location = 3) out vec4 WorldPos;
layout(location = 4) out vec4 SplatId;
layout(location = 5) out vec3 Normal;
layout(location = 6) flat out int instanceID;

void main()
{
    VERTEX_SETUP_INSTANCING;
    instanceID = gl_InstanceIndex;

    WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
    Color = color;
    TexCoord = uv1;
    TexCoord2 = uv2;
    Normal = mat3(transpose(inverseModel)) * normalize(normal.xyz);
}
