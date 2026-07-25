#version 450
#include "theengine.cginc"

// Camera-facing billboard for NPC status icons (quest !, quest ?, gossip, AI...) drawn above the
// nameplate. World-sized like the name text; when an NPC has several icons they are laid out
// horizontally, centered above the anchor. Per instance: model = translation to the anchor point,
// drawDataX = icon type (drawn procedurally in status_icon.frag), drawDataY = slot index,
// drawDataZ = total icons on this NPC.

layout(location = 0) out vec2 TexCoords;
layout(location = 1) flat out int vIconType;

const float IconSize = 0.7;     // world height/width of one icon
const float IconSpacing = 0.78; // horizontal step between icons of one NPC

void main()
{
    VERTEX_SETUP_INSTANCING;
    vIconType = drawDataX;

    // anchor -> view space; the quad offsets are added in view space (rotation-free), so the
    // quad always faces the camera - same technique as world_text.vert / gizmo_icon.vert
    vec4 viewCenter = view * model * vec4(0.0, 0.0, 0.0, 1.0);
    float x = (float(drawDataY) - float(drawDataZ - 1) * 0.5) * IconSpacing;
    // position is a [-0.5, 0.5]^2 quad; +0.5 on y puts the icon's bottom edge at the anchor
    vec4 viewPos = viewCenter + vec4(position.x * IconSize + x, (position.y + 0.5) * IconSize, 0.0, 0.0);
    gl_Position = projection * viewPos;
    TexCoords = uv1;
}
