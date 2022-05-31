#version 330 core
#include "../internalShaders/theengine.cginc"

uniform vec4 color;
out vec4 FragColor;

void main()
{   
    FragColor = color;
}