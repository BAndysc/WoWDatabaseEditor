#version 330 core
#include "../internalShaders/theengine.cginc"

out vec2 TexCoord;
out vec3 WorldNormal;

void main()
{
    VERTEX_SETUP_INSTANCING;

    vec4 worldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * worldPos;
    TexCoord = uv1;
    // uniform scale, so the model matrix rotates normals correctly
    WorldNormal = normalize(mat3(model) * normal);
}
