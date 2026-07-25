#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) out vec2 TexCoord;
#ifndef DEPTH_PASS
layout(location = 1) out vec3 WorldNormal;
layout(location = 2) out vec3 WorldPos;
layout(location = 3) flat out int instanceID;
#endif

void main()
{
    VERTEX_SETUP_INSTANCING;

    vec4 worldPos = model * vec4(position.xyz, 1.0);
    // identical clip position in every pass so the depth pass matches the forward pass for early-Z
    gl_Position = projection * view * worldPos;
    TexCoord = uv1;
#ifndef DEPTH_PASS
    instanceID = gl_InstanceIndex;
    // uniform scale, so the model matrix rotates normals correctly
    WorldNormal = normalize(mat3(model) * normal);
    WorldPos = worldPos.xyz;
#endif
}
