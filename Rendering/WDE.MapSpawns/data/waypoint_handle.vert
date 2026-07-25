#version 450
#include "../internalShaders/theengine.cginc"

// Vertex-pulling camera-facing dots for each waypoint. The bound storage buffer holds, per point,
// 2 vec4 entries: [pos.xyz, sizePx], [color.rgba]. Each point emits 6 vertices (a screen-space
// quad of constant pixel size); the fragment shader clips it into a filled circle.
layout(std430, set = 1, binding = 1) readonly buffer HandleBuffer { vec4 handles[]; };

layout(location = 0) out vec4 vColor;
layout(location = 1) out vec2 vOff;

void main()
{
    int vid = gl_VertexIndex;
    int p = vid / 6;
    int corner = vid % 6;

    vec4 d0 = handles[p * 2 + 0];
    vec3 pos = d0.xyz;
    float sizePx = d0.w;
    vColor = handles[p * 2 + 1];

    vec2 corners[6] = vec2[6](
        vec2(-1.0, -1.0), vec2(1.0, -1.0), vec2(1.0, 1.0),
        vec2(-1.0, -1.0), vec2(1.0, 1.0), vec2(-1.0, 1.0)
    );
    vOff = corners[corner];

    vec4 clip = projection * view * vec4(pos, 1.0);
    vec2 res = vec2(screenWidth, screenHeight);
    clip.xy += (vOff * sizePx / res * 2.0) * clip.w;
    gl_Position = clip;
}
