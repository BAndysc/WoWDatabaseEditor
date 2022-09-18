#version 330 core
#include "../internalShaders/theengine.cginc"

out vec3 ScreenPos;
out vec4 WorldPos;

void main()
{
    WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
    ScreenPos = (gl_Position.xyz / gl_Position.w) * 0.5 + 0.5;
    
}