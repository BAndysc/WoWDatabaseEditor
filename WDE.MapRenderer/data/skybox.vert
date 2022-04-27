#version 330 core
#include "../internalShaders/theengine.cginc"

out vec2 TexCoord;
out vec3 WorldPos;
out vec4 Normal;

void main()
{
    WorldPos = (model * vec4(position.xyz, 1)).xyz;
    gl_Position = projection * mat4(mat3(view)) * vec4(WorldPos.xyz, 1.0);
    TexCoord = uv1;
    Normal = vec4(normalize(0-WorldPos.xyz), 0);
}