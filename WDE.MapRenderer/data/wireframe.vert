#version 330 core
#include "../internalShaders/theengine.cginc"

void main()
{
    VERTEX_SETUP_INSTANCING;

    vec4 vWorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * vWorldPos;
}