#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec2 TexCoords;

void main()
{
    gl_Position = vec4(position.x, position.y, 0.0, 1.0);
    TexCoords = uv1;
}
