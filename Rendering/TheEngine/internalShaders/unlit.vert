#version 330 core
#include "../internalShaders/theengine.cginc"

void main()
{
    vec4 WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
}