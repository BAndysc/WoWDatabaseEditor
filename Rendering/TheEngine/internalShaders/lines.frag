#version 330 core
#include "theengine.cginc"

in vec4 lineColor;
out vec4 FragColor;

void main()
{
    FragColor = lineColor;
}
