#version 330 core
layout (location = 0) in vec4 position;
layout (location = 1) in vec4 color;
layout (location = 2) in vec4 normal;
layout (location = 3) in vec2 uv1;
layout (location = 4) in vec2 uv2;

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

layout (std140) uniform ObjectData
{
#ifdef Instancing
    mat4 _model;
    mat4 _inverseModel;
#else
    mat4 model;
    mat4 inverseModel;
#endif
};

out vec4 TexCoord;

void main()
{
    gl_Position = projection * mat4(mat3(view)) * model * vec4(position.xyz, 1.0);
    TexCoord = vec4(uv1, 0, 0);
}