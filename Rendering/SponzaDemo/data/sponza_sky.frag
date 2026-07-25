#version 330 core
#include "../internalShaders/theengine.cginc"

uniform sampler2D texture1;

in vec2 TexCoord;
out vec4 FragColor;

void main()
{
    FragColor = vec4(texture(texture1, TexCoord).rgb, 1.0);
}
