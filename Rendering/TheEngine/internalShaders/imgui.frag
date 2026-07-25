#version 450
#extension GL_EXT_nonuniform_qualifier : require
// Bindless: the texture for each ImGui draw command arrives as a packed index push constant
// (see ImGuiController + GetBindlessIndex), so set 1 holds only the projection UBO and is bound
// once for the whole UI instead of being rewritten on every texture change.
layout(set = 2, binding = 0) uniform texture2D bindlessTextures[16384];
layout(set = 2, binding = 1) uniform sampler bindlessSamplers[128];
layout(push_constant) uniform PushConstants { int textureIndex; };

layout (location = 0) in vec4 color;
layout (location = 1) in vec2 texCoord;

layout (location = 0) out vec4 outputColor;

void main()
{
    // packed index = (samplerSlot << 20) | textureSlot
    vec4 sampled = texture(sampler2D(bindlessTextures[nonuniformEXT(textureIndex & 0xFFFFF)], bindlessSamplers[nonuniformEXT(textureIndex >> 20)]), texCoord);
    outputColor = color * sampled;
}
