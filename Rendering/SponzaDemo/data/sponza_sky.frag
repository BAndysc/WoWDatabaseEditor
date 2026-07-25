#version 450
#include "../internalShaders/theengine.cginc"

struct MaterialData
{
    vec4 diffuseColor;
    float alphaCutoff;
    int useMask;
    int texture1Index;
    int maskTextureIndex;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

layout(location = 0) in vec2 TexCoord;

#ifdef DEPTH_PASS

// the sky is opaque, so the depth pass writes only depth (no color). Touch TexCoord + MATERIAL_INDEX
// so the vertex outputs stay matched (no unused-output warnings); the discard never triggers.
void main()
{
    if (SAMPLE_BINDLESS(M(texture1Index), TexCoord).a < 0.0)
        discard;
}

#else

layout(location = 0) out vec4 FragColor;

void main()
{
    FragColor = vec4(SAMPLE_BINDLESS(M(texture1Index), TexCoord).rgb, 1.0);
}

#endif
