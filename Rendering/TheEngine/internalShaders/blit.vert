#version 330 core
layout (location = 0) in vec4 position;
layout (location = 1) in vec4 color;
layout (location = 2) in vec4 normal;
layout (location = 3) in vec2 uv1;
layout (location = 4) in vec2 uv2;

out vec2 TexCoords;

void main()
{
    gl_Position = vec4(position.x, position.y, 0.0, 1.0);
    TexCoords = uv1;
}