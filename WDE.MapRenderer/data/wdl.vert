#version 450
#include "../internalShaders/theengine.cginc"

#ifndef DEPTH_PASS
layout(location = 0) flat out int instanceID;
#endif

void main()
{
    VERTEX_SETUP_INSTANCING;

    vec4 worldPos = model * vec4(position.xyz, 1.0);
    gl_Position = projection * view * worldPos;
#ifndef DEPTH_PASS
    instanceID = gl_InstanceIndex;
#endif
}
