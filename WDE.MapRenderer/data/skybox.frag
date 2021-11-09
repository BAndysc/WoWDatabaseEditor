#version 330 core
#include "../internalShaders/theengine.cginc"
in vec4 TexCoord;
out vec4 FragColor;

uniform sampler2D texture1;

void main()
{
    vec4 color = texture(texture1, TexCoord.xy);

    FragColor = vec4(color.rgb, 1.0f);
}