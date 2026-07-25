#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) in vec4 Color;
layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec2 TexCoord2;
layout(location = 3) in vec4 WorldPos;
layout(location = 4) in vec4 SplatId;
layout(location = 5) in vec3 Normal;
layout(location = 6) flat in int instanceID;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;

struct MaterialData
{
    vec4 mesh_color;
    float alphaTest;
    int texture1Index;
    int ufwmo_pad1;
    int ufwmo_pad2;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

void main()
{
    vec4 tex1 = SAMPLE_BINDLESS(M(texture1Index), TexCoord.xy);

    if (tex1.a < M(alphaTest))
        discard;

    FragColor = M(mesh_color);
}
