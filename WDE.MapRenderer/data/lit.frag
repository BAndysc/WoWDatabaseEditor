#version 330 core
#include "../internalShaders/theengine.cginc"
in vec4 Color;
in vec4 TexCoord;
in vec4 WorldPos;
flat in ivec4 SplatId;
in vec4 Normal;
flat in int ChunkId;
out vec4 FragColor;

uniform sampler2DArray texture1;
uniform sampler2DArray holes;
uniform sampler2D _tex0;
uniform sampler2D _tex1;
uniform sampler2D _tex2;
uniform sampler2D _tex3;
uniform sampler2D _tex4;
uniform sampler2D _tex5;
uniform sampler2D _tex6;
uniform sampler2D _tex7;
uniform sampler2D _tex8;
uniform sampler2D _tex9;
uniform sampler2D _tex10;
uniform sampler2D _tex11;
uniform sampler2D _tex12;
uniform sampler2D _tex13;

const int FOG_START = 1400;
const int FOG_END = 1800;
const vec3 FOG_COLOR = vec3(1, 1, 1);

vec4 sample(int idx)
{
    vec2 uv = TexCoord.xy * 8;
    if (idx == 1)
        return texture(_tex1, uv);
    if (idx == 2)
        return texture(_tex2, uv);
    if (idx == 3)
        return texture(_tex3, uv);
    if (idx == 4)
        return texture(_tex4, uv);
    if (idx == 5)
        return texture(_tex5, uv);
    if (idx == 6)
        return texture(_tex6, uv);
    if (idx == 7)
        return texture(_tex7, uv);
    if (idx == 8)
        return texture(_tex8, uv);
    if (idx == 9)
        return texture(_tex9, uv);
    if (idx == 10)
        return texture(_tex10, uv);
    if (idx == 11)
        return texture(_tex11, uv);
    if (idx == 12)
        return texture(_tex12, uv);
    if (idx == 13)
        return texture(_tex13, uv);
    return texture(_tex0, uv);
}

void main()
{
    vec4 hole = texture(holes, vec3(TexCoord.x, TexCoord.y, ChunkId));//vec2(TexCoord.x / 16 + (ChunkId / 16) / 16.0, TexCoord.y / 16 + (ChunkId % 16) / 16.0));
    vec4 colSplat = texture(texture1, vec3(TexCoord.x, TexCoord.y, ChunkId));
    
    if (hole.r > 0.5)
        discard;
    
    vec4 col1 = sample(SplatId.r);
    vec4 col2 = sample(SplatId.g);
    vec4 col3 = sample(SplatId.b);
    vec4 col4 = sample(SplatId.a);
    FragColor = vec4((1-colSplat.r-colSplat.g-colSplat.b) * col1.rgb + colSplat.r * col2.rgb + colSplat.g * col3.rgb + colSplat.b * col4.rgb, 1.0f);
    
    float shadow = 0;
    for (int x = -3; x <= 3; ++x)
    {
        for (int y = -3; y <= 3; ++y)
        {
            vec4 sampled = texture(texture1, vec3(TexCoord.x - x / 64.0f, TexCoord.y - y /64.0f, ChunkId));
            shadow += sampled.a;
        }
    }
    shadow /= 81;
    vec4 shadowed = vec4(FragColor.rgb * 0.5, 1);
    FragColor = mix(FragColor, shadowed, shadow);
   
    float diff = max(dot(Normal.xyz, -lightDir.xyz), 0.0);
    vec3 diffuse = diff * lightColor.rgb;
    vec3 ambient = vec3(1, 1, 1) * 0.4;
    FragColor = vec4(FragColor.rgb * (diffuse + ambient), 1);
    
    float dist = distance(WorldPos, cameraPos);
    float fogFactor = (clamp(dist, FOG_START, FOG_END) - FOG_START) / (FOG_END - FOG_START);
    //FragColor = vec4(mix(FragColor.rgb, FOG_COLOR, fogFactor), 1);
}