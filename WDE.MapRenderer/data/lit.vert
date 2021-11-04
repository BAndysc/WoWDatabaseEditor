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

//uniform sampler2D heights;
uniform samplerBuffer heightsNormalBuffer;
uniform sampler2D chunkToSplat;
//uniform sampler2D heightsRealTexture;

layout (std140) uniform ObjectData
{
    mat4 model;
    mat4 inverseModel;
};

//uniform mat4 model;
//uniform mat4 view;
//uniform mat4 projection;

out vec4 Color;
out vec4 TexCoord;
out vec4 WorldPos;
out vec4 SplatId;
out vec4 Normal;
flat out int ChunkId;

void main()
{
    vec4 normalHeight = texelFetch(heightsNormalBuffer, gl_VertexID);
    //vec4 normalHeight = textureLod(heights, vec2((gl_VertexID % 256) / 256.0, (gl_VertexID / 256) / 256.0), 0);
    //h = textureLod(heightsRealTexture, uv1, 0);
    
    SplatId = textureLod(chunkToSplat, vec2((gl_VertexID / 145) / 256.0, 0), 0);
     
    ChunkId = gl_VertexID / 145;
    
    Normal = vec4(normalize(normalHeight.xyz), 1);
    WorldPos = model * vec4(position.xyz + vec3(0, normalHeight.w, 0), 1.0);
    gl_Position = projection * view * WorldPos;
    Color = color;
    TexCoord = vec4(uv1, 0, 0);
}