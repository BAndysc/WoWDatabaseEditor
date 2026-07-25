#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec4 Color;
layout(location = 1) out vec4 TexCoord;
layout(location = 2) out vec4 WorldPos;
layout(location = 3) out vec4 SplatId;
layout(location = 4) out vec3 Normal;

void main()
{
    VERTEX_SETUP_INSTANCING;
    WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
    Color = color;
    TexCoord = vec4(uv1, 0.0, 0.0);
    Normal = normalize(mat3(transpose(inverseModel)) * normalize(normal.xyz));
}
