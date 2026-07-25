#version 450
#include "theengine.cginc"

// Camera-facing billboard for editor scene-view gizmo icons (lights, cameras, decals).
// The quad keeps a constant on-screen size: the per-vertex offset is added in view space and
// scaled by the gizmo's view-space distance, which cancels the perspective divide.

layout(location = 0) out vec2 vP;          // quad coords in [-1, 1], used by the icon SDFs
layout(location = 1) flat out int iconType; // 0 = light, 1 = camera, 2 = decal (from drawDataX)
layout(location = 2) flat out vec4 tint;    // RGBA tint, packed RGBA8 in drawDataY
layout(location = 3) flat out int vPickId;  // object-picker id of the entity this icon represents (drawDataZ; 0 = none)

void main()
{
    VERTEX_SETUP_INSTANCING;

    iconType = drawDataX;
    vPickId = drawDataZ;
    uint packed = uint(drawDataY);
    tint = vec4(float(packed & 0xFFu),
                float((packed >> 8) & 0xFFu),
                float((packed >> 16) & 0xFFu),
                float((packed >> 24) & 0xFFu)) / 255.0;

    // gizmo origin -> view space (robust to matrix layout conventions: transform the origin
    // rather than reading a translation column).
    vec4 viewCenter = view * model * vec4(0.0, 0.0, 0.0, 1.0);

    float dist = max(-viewCenter.z, 0.001); // positive distance in front of the camera
    float scale = 0.045 * dist;             // constant screen size (see header)

    vec4 viewPos = viewCenter + vec4(position.xy * scale, 0.0, 0.0);
    gl_Position = projection * viewPos;

    vP = position.xy;
}
