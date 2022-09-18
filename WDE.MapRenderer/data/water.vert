#version 330 core
#include "../internalShaders/theengine.cginc"

out vec3 ScreenPos;
out vec4 WorldPos;

void main()
{
    WorldPos = model * vec4(position.xyz - vec3(0, 0, - sin(time/ 1000) * 0.1), 1.0);
    gl_Position = projection * view * WorldPos;
    ScreenPos = (gl_Position.xyz / gl_Position.w) * 0.5 + 0.5;
    
}