#version 450
#include "../internalShaders/theengine.cginc"

layout(std430, set = 3, binding = 0) readonly buffer boneMatrices { mat4 boneMatricesData[]; };

layout(location = 0) out vec4 Color;
layout(location = 1) out vec2 TexCoord;
layout(location = 2) out vec2 TexCoord2;
layout(location = 3) out vec4 WorldPos;
layout(location = 4) out vec4 SplatId;
layout(location = 5) out vec3 Normal;

void main()
{
    VERTEX_SETUP_INSTANCING;

    int boneIndices[4];
    boneIndices[0] = int(color2.x * 255.0);
    boneIndices[1] = int(color2.y * 255.0);
    boneIndices[2] = int(color2.z * 255.0);
    boneIndices[3] = int(color2.w * 255.0);
    vec4 boneWeights = color;

    // clamp into the bound buffer: MoltenVK/Metal has no robustBufferAccess for SSBOs, so an
    // index past boneMatricesData (bad model data, or a model with no bones) is a hard GPU page fault.
    // boneCount == 0 (the 4-byte dummy SSBO bound for an M2 with no/unbound bone buffer) makes
    // clamp(x, 0, boneCount - 1) == clamp(x, 0, -1) == -1 -> boneMatricesData[-1], a backward OOB
    // GPU page fault on MoltenVK. Guard it explicitly.
    int boneCount = boneMatricesData.length();
    mat4 boneMatricesArray[4];
    for (int i = 0; i < 4; i++)
    {
        boneMatricesArray[i] = boneCount > 0
            ? boneMatricesData[clamp(drawDataW + boneIndices[i], 0, boneCount - 1)]
            : mat4(1.0);
    }

    mat4 boneTransform = boneMatricesArray[0] * boneWeights[0] + boneMatricesArray[1] * boneWeights[1] + boneMatricesArray[2] * boneWeights[2] + boneMatricesArray[3] * boneWeights[3];

    WorldPos = model * (boneTransform * vec4(position.xyz, 1.0));
    gl_Position = projection * view * WorldPos;
    Color = vec4(boneIndices[0] / 255.0, boneIndices[1] / 255.0, boneIndices[2] / 255.0, boneIndices[3] / 255.0);
    TexCoord = uv1;
    TexCoord2 = uv2;
    Normal = mat3(transpose(inverseModel)) * normalize(normal.xyz);
}
