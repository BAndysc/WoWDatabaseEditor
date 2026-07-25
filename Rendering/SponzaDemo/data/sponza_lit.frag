#version 330 core
#include "../internalShaders/theengine.cginc"

uniform sampler2D texture1;
uniform sampler2D maskTexture;

uniform vec4 diffuseColor;
uniform float alphaCutoff;
uniform int useMask;

in vec2 TexCoord;
in vec3 WorldNormal;
out vec4 FragColor;

void main()
{
    vec4 tex = texture(texture1, TexCoord);
    // foliage/chains carry their cutout in a separate mask texture (wavefront map_d)
    float alpha = useMask == 1 ? texture(maskTexture, TexCoord).r : tex.a;
    if (alpha < alphaCutoff)
        discard;
    FragColor = vec4(lighting(tex.rgb * diffuseColor.rgb, normalize(WorldNormal)), 1.0);
}
