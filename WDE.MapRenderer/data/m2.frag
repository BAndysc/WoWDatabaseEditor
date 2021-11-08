#version 330 core
in vec4 Color;
in vec4 TexCoord;
in vec4 WorldPos;
in vec4 SplatId;
in vec3 Normal;
out vec4 FragColor;

layout (std140) uniform SceneData
{
    mat4 view;
    mat4 projection;
    vec4 cameraPos;
    vec4 lightDir;
    vec4 lightColor;
    vec3 lightPosition;
    float padding;
    float time;
    float padding2[3];
};

uniform sampler2D texture1;
uniform float alphaTest;
uniform float notSupported;
uniform float highlight;

void main()
{
    float diff = max(dot(Normal, -lightDir.xyz), 0.0);
    vec3 diffuse = diff * lightColor.rgb;
    vec3 ambient = vec3(1, 1, 1) * 0.4;
    vec4 color = texture(texture1, TexCoord.xy);

    if (color.a < alphaTest) 
        discard;
        
    FragColor = vec4(color.rgb * (diffuse + ambient), mix(1, color.a, (sign(alphaTest) + 1) / 2));
    
    vec4 highglighted = FragColor + vec4(0.1, 0.1, 0.1, 0);
    FragColor = mix(FragColor, highglighted, highlight);
    
    FragColor = vec4(mix(FragColor.rgb, vec3(1, 0, 0), notSupported), FragColor.a);
}