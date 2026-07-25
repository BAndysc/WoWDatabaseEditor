#version 450
#include "../internalShaders/theengine.cginc"

// Vertex-pulling thick-path renderer. No vertex attributes are read. The bound storage buffer
// holds, per segment, 3 vec4 entries: [A.xyz, _], [B.xyz, _], [color.rgba]. Each segment emits 9
// vertices: 6 for a screen-space-thick ribbon quad (A->B) and 3 for a direction arrowhead at
// the segment midpoint. Thickness/arrow size are constant in pixels regardless of distance.
layout(std430, set = 1, binding = 1) readonly buffer WaypointBuffer { vec4 waypoints[]; };
// params.y = 1 to emit direction arrowheads, 0 to suppress them (the ground shadow reuses this
// shader but wants a plain ribbon). params.x is the fragment alpha multiplier (see the .frag).
layout(std430, set = 1, binding = 2) readonly buffer WaypointParams { vec4 params; };

layout(location = 0) out vec4 vColor;

const float RIBBON_HALF_PX = 4.0;
const float ARROW_PX = 10.0;

vec4 applyPx(vec4 clip, vec2 px, vec2 res)
{
    clip.xy += (px / res * 2.0) * clip.w;
    return clip;
}

void main()
{
    int vid = gl_VertexIndex;
    int seg = vid / 9;
    int corner = vid % 9;
    int base = seg * 3;

    vec3 A = waypoints[base + 0].xyz;
    vec3 B = waypoints[base + 1].xyz;
    vColor = waypoints[base + 2];

    vec4 clipA = projection * view * vec4(A, 1.0);
    vec4 clipB = projection * view * vec4(B, 1.0);
    vec4 clipM = (clipA + clipB) * 0.5;

    vec2 res = vec2(screenWidth, screenHeight);
    vec2 ndcA = clipA.xy / clipA.w;
    vec2 ndcB = clipB.xy / clipB.w;
    vec2 dirPx = (ndcB - ndcA) * res;
    float len = max(length(dirPx), 0.0001);
    vec2 dir = dirPx / len;
    vec2 nrm = vec2(-dir.y, dir.x);

    vec4 clip;
    vec2 offPx;
    if (corner == 0)      { clip = clipA; offPx = -nrm * RIBBON_HALF_PX; }
    else if (corner == 1) { clip = clipA; offPx =  nrm * RIBBON_HALF_PX; }
    else if (corner == 2) { clip = clipB; offPx = -nrm * RIBBON_HALF_PX; }
    else if (corner == 3) { clip = clipB; offPx = -nrm * RIBBON_HALF_PX; }
    else if (corner == 4) { clip = clipA; offPx =  nrm * RIBBON_HALF_PX; }
    else if (corner == 5) { clip = clipB; offPx =  nrm * RIBBON_HALF_PX; }
    // arrowhead (corners 6-8); when arrows are off, collapse all three to clipM so the triangle has
    // zero area and rasterizes nothing
    else if (corner == 6) { clip = clipM; offPx = params.y > 0.5 ?  dir * ARROW_PX : vec2(0.0); }
    else if (corner == 7) { clip = clipM; offPx = params.y > 0.5 ? -dir * ARROW_PX + nrm * ARROW_PX : vec2(0.0); }
    else                  { clip = clipM; offPx = params.y > 0.5 ? -dir * ARROW_PX - nrm * ARROW_PX : vec2(0.0); }

    gl_Position = applyPx(clip, offPx, res);
}
