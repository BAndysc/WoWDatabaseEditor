#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec3 ScreenPos;
layout(location = 1) out vec4 WorldPos;

void main()
{
    VERTEX_SETUP_INSTANCING;

    WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
    ScreenPos = (gl_Position.xyz / gl_Position.w) * 0.5 + 0.5;
}
