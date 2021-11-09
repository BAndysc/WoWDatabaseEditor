#version 330 core
#include "../internalShaders/theengine.cginc"

out vec4 TexCoord;

void main()
{
    gl_Position = projection * mat4(mat3(view)) * model * vec4(position.xyz, 1.0);
    TexCoord = vec4(uv1, 0, 0);
}