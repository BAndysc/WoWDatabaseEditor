#version 330 core
#include "../internalShaders/theengine.cginc"

out vec4 Color;
out vec2 TexCoord;
out vec2 TexCoord2;
out vec4 WorldPos;
out vec4 SplatId;
out vec3 Normal;

uniform samplerBuffer boneMatrices;

void main()
{
    VERTEX_SETUP_INSTANCING;

    uint packedBoneIndices = floatBitsToUint(color.r);
    uint packedBoneWeights = floatBitsToUint(color.g);
    
    int boneIndices[4];
    boneIndices[0] = floatBitsToInt(uintBitsToFloat( packedBoneIndices & 0xFFu));
    boneIndices[1] = floatBitsToInt(uintBitsToFloat( (packedBoneIndices >> 8) & 0xFFu));
    boneIndices[2] = floatBitsToInt(uintBitsToFloat( (packedBoneIndices >> 16) & 0xFFu));
    boneIndices[3] = floatBitsToInt(uintBitsToFloat( (packedBoneIndices >> 24) & 0xFFu));
    
    float boneWeights[4];
    boneWeights[0] = (packedBoneWeights & 0xFFu) / 255.0f;
    boneWeights[1] = ((packedBoneWeights >> 8) & 0xFFu) / 255.0f;
    boneWeights[2] = ((packedBoneWeights >> 16) & 0xFFu) / 255.0f;
    boneWeights[3] = ((packedBoneWeights >> 24) & 0xFFu) / 255.0f;

    mat4 boneMatricesArray[4];
    for (int i = 0; i < 4; i++)
    {
        boneMatricesArray[i] = mat4(texelFetch(boneMatrices, boneIndices[i] * 4), texelFetch(boneMatrices, boneIndices[i] * 4 + 1), texelFetch(boneMatrices, boneIndices[i] * 4 + 2), texelFetch(boneMatrices, boneIndices[i] * 4 + 3));  
    }

    mat4 boneTransform = boneMatricesArray[0] * boneWeights[0] + boneMatricesArray[1] * boneWeights[1] + boneMatricesArray[2] * boneWeights[2] + boneMatricesArray[3] * boneWeights[3];

    WorldPos = model * (boneTransform * vec4(position.xyz, 1.0)); // * boneTransform
    gl_Position = projection * view * WorldPos;
    Color = vec4(boneIndices[0] / 255.0f, boneIndices[1] / 255.0f, boneIndices[2] / 255.0f, boneIndices[3] / 255.0f);
    TexCoord = uv1;
    TexCoord2 = uv2;
    Normal = mat3(transpose(inverseModel)) * normalize(normal.xyz);
}