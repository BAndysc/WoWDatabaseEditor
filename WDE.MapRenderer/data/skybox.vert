#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec2 TexCoord;
layout(location = 1) out vec3 WorldPos;
layout(location = 2) out vec4 Normal;

void main()
{
    VERTEX_SETUP_INSTANCING;
    WorldPos = (model * vec4(position.xyz, 1.0)).xyz;
    gl_Position = projection * mat4(mat3(view)) * vec4(WorldPos.xyz, 1.0);
    TexCoord = uv1;
    Normal = vec4(normalize(0.0-WorldPos.xyz), 0.0);
}
