#version 330 core
in vec4 TexCoord;
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

void main()
{
    vec4 color = texture(texture1, TexCoord.xy);

    FragColor = vec4(color.rgb, 1.0f);
}