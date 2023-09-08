#version 330 core
#include "../internalShaders/theengine.cginc"

layout (location = 0) out vec4 FragColor;

void main()
{
    FragColor = fogColor;
}