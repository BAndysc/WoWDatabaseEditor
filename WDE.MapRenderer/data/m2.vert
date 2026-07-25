#version 450
#include "../internalShaders/theengine.cginc"

layout(std430, set = 3, binding = 0) readonly buffer boneMatrices { mat4 boneMatricesData[]; };
layout(std430, set = 3, binding = 1) readonly buffer vertexColors { vec4 vertexColorsData[]; };
layout(std430, set = 3, binding = 2) readonly buffer textureTransforms { mat4 textureTransformsData[]; };

// TexCoord/TexCoord2/VertexColor feed the combiner that produces the cutout alpha, so the depth
// pass needs them too; the rest is forward-only.
layout(location = 1) out vec2 TexCoord;
layout(location = 2) out vec2 TexCoord2;
layout(location = 6) out vec4 VertexColor;
#ifndef DEPTH_PASS
layout(location = 0) out vec4 Color;
layout(location = 3) out vec4 WorldPos;
layout(location = 4) out vec4 SplatId;
layout(location = 5) out vec3 Normal;
layout(location = 7) flat out int instanceID;
#endif

void main()
{
    VERTEX_SETUP_INSTANCING;

    // upper-bound the indices too (see boneCount note below): Metal won't catch an overrun.
    VertexColor = (drawDataX < 0 || drawDataX >= vertexColorsData.length()) ? vec4(1.0, 1.0, 1.0, 1.0) : vertexColorsData[drawDataX];

    mat4 textureTransformsArray[2];
    int textureTransformCount = textureTransformsData.length();
    for (int i = 0; i < 2; i++)
    {
        int textureTransformIndex = i == 0 ? drawDataY : drawDataZ;
        if (textureTransformIndex >= 0 && textureTransformIndex < textureTransformCount)
        {
            textureTransformsArray[i] = textureTransformsData[textureTransformIndex];
        }
        else
        {
            textureTransformsArray[i] = mat4(1.0);
        }
    }

    int boneIndices[4];
    boneIndices[0] = int(color2.x * 255.0);
    boneIndices[1] = int(color2.y * 255.0);
    boneIndices[2] = int(color2.z * 255.0);
    boneIndices[3] = int(color2.w * 255.0);
    vec4 boneWeights = color;

    // clamp into the bound buffer: MoltenVK/Metal has no robustBufferAccess, so an index past
    // boneMatricesData (bad model data, or a model with no bones) is a hard GPU page fault.
    // boneCount == 0 (the 4-byte dummy SSBO is bound for an M2 with no/unbound bone buffer) MUST be
    // handled explicitly: clamp(x, 0, boneCount - 1) becomes clamp(x, 0, -1) == -1, i.e. a negative
    // read boneMatricesData[-1] - a backward OOB GPU page fault. Guard it like vertexColors/textureTransforms.
    int boneCount = boneMatricesData.length();
    mat4 boneMatricesArray[4];
    for (int i = 0; i < 4; i++)
    {
        boneMatricesArray[i] = boneCount > 0
            ? boneMatricesData[clamp(drawDataW + boneIndices[i], 0, boneCount - 1)]
            : mat4(1.0);
    }

    mat4 boneTransform = boneMatricesArray[0] * boneWeights[0] + boneMatricesArray[1] * boneWeights[1] + boneMatricesArray[2] * boneWeights[2] + boneMatricesArray[3] * boneWeights[3];

    // identical skinned clip position in every pass so the depth pass matches the forward pass
    vec4 worldPos = model * (boneTransform * vec4(position.xyz, 1.0));
    gl_Position = projection * view * worldPos;
    TexCoord = (textureTransformsArray[0] * vec4(uv1, 0.0, 1.0)).xy;
    TexCoord2 = (textureTransformsArray[1] * vec4(uv2, 0.0, 1.0)).xy;
#ifndef DEPTH_PASS
    instanceID = gl_InstanceIndex;
    WorldPos = worldPos;
    Color = vec4(boneIndices[0] / 255.0, boneIndices[1] / 255.0, boneIndices[2] / 255.0, boneIndices[3] / 255.0);
    Normal = mat3(transpose(inverseModel)) * normalize(normal.xyz);
#endif
}
