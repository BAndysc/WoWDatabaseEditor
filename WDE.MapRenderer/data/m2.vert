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

#ifdef Instancing
    uniform samplerBuffer InstancingModels;
    uniform samplerBuffer InstancingInverseModels;
#endif

out vec4 Color;
out vec4 TexCoord;
out vec4 WorldPos;
out vec4 SplatId;
out vec3 Normal;

void main()
{     
#ifdef Instancing
    mat4 model = mat4(texelFetch(InstancingModels, gl_InstanceID * 4),
                        texelFetch(InstancingModels, gl_InstanceID * 4 + 1),
                        texelFetch(InstancingModels, gl_InstanceID * 4 + 2),
                        texelFetch(InstancingModels, gl_InstanceID * 4 + 3));
    mat4 inverseModel = mat4(texelFetch(InstancingInverseModels, gl_InstanceID * 4),
                        texelFetch(InstancingInverseModels, gl_InstanceID * 4 + 1),
                        texelFetch(InstancingInverseModels, gl_InstanceID * 4 + 2),
                        texelFetch(InstancingInverseModels, gl_InstanceID * 4 + 3)); 
#endif

    WorldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * WorldPos;
    Color = color;
    TexCoord = vec4(uv1, 0, 0);
    Normal = mat3(transpose(inverseModel)) * normalize(normal.xyz);
}