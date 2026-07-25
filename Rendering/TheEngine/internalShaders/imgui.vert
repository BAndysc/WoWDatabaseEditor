#version 450

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    mat4 projection_matrix;
};

layout (location = 0) in vec2 in_position;
layout (location = 1) in vec2 in_texCoord;
layout (location = 2) in vec4 in_color;

layout (location = 0) out vec4 color;
layout (location = 1) out vec2 texCoord;

void main()
{
    // ortho projection already emits native Vulkan depth (ndc z in [0,1]); no VK_CLIP remap
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}
