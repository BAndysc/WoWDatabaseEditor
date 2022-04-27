#version 330 core
#include "../internalShaders/theengine.cginc"

//uniform sampler2D heights;
//uniform sampler2D heightsRealTexture;
uniform samplerBuffer heightsNormalBuffer;
uniform isamplerBuffer chunkToSplat;

out vec4 Color;
out vec4 TexCoord;
out vec4 WorldPos;
flat out ivec4 SplatId;
out vec4 Normal;
flat out int ChunkId;

void main()
{
    vec4 normalHeight = texelFetch(heightsNormalBuffer, gl_VertexID);
    //vec4 normalHeight = textureLod(heights, vec2((gl_VertexID % 256) / 256.0, (gl_VertexID / 256) / 256.0), 0);
    //h = textureLod(heightsRealTexture, uv1, 0);
    
    SplatId = texelFetch(chunkToSplat, gl_VertexID / 145);
     
    ChunkId = gl_VertexID / 145;
    
    Normal = vec4(normalize(normalHeight.xyz), 1);
    WorldPos = model * vec4(position.xyz + vec3(0, 0, normalHeight.w), 1.0);
    gl_Position = projection * view * WorldPos;
    Color = color;
    TexCoord = vec4(uv1, 0, 0);
}