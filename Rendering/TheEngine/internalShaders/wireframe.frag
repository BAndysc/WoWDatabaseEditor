#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec4 FragColor;

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    vec4 color;
    float width;
    int wf_padding1;
    int wf_padding2;
    int wf_padding3;
};

// MoltenVK has no geometry shaders, so the GL barycentric trick is gone: the
// pipeline rasterizes in line polygon mode instead and the fragment is plain color
void main()
{
    FragColor = color;
}
