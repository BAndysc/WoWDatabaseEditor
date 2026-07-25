#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 1) out vec2 TexCoord;
#ifndef DEPTH_PASS
layout(location = 0) out vec4 Color;
layout(location = 2) out vec2 TexCoord2;
layout(location = 3) out vec4 WorldPos;
layout(location = 4) out vec4 SplatId;
layout(location = 5) out vec3 Normal;
layout(location = 6) flat out int instanceID;
#endif

void main()
{
    VERTEX_SETUP_INSTANCING;

    vec4 worldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * worldPos;
    TexCoord = uv1;
#ifndef DEPTH_PASS
    instanceID = gl_InstanceIndex;
    WorldPos = worldPos;
    Color = color;
    TexCoord2 = uv2;
    Normal = mat3(transpose(inverseModel)) * normalize(normal.xyz);
#endif
}
