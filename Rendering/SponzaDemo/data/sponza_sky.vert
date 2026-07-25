#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec2 TexCoord;

void main()
{
    VERTEX_SETUP_INSTANCING;

    vec4 worldPos = model * vec4(position.xyz, 1.0);
    vec4 clip = projection * view * worldPos;
    // depth = 1.0: the sky only fills pixels no geometry covered (with LessEqual test)
    gl_Position = clip.xyww;
    TexCoord = uv1;
}
