#version 450
#include "../internalShaders/theengine.cginc"

#ifdef DEPTH_PASS

void main()
{
    // opaque distant terrain: depth only, no cutout. Touch vMatIdx so the vertex's flat output
    // (set by VERTEX_SETUP_INSTANCING) stays consumed; the discard never triggers.
    if (vMatIdx < -2000000000)
        discard;
}

#else

layout(location = 0) flat in int instanceID;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;

void main()
{
    PIXEL_SETUP_INSTANCING(instanceID);
    FragColor = fogColor;
    ObjectIndexOutputBuffer = objectIndex;
}

#endif
