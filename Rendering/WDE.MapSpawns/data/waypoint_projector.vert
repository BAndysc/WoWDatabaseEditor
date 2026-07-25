#version 450
#include "../internalShaders/theengine.cginc"

// Top-down projector pass: renders the path as flat world-space ribbons into the projector overlay
// texture (which surface shaders/decals then project onto terrain). Unlike waypoint_path.vert this
// expands the ribbon by a constant WORLD width (the projection is orthographic top-down) and uses
// its own ortho matrix from ProjectorParams - it ignores the camera SceneData entirely. No arrows.
layout(std430, set = 1, binding = 1) readonly buffer WaypointBuffer { vec4 waypoints[]; };
// projViewProj = top-down ortho world->clip; projMisc.x = target on-screen half-width in PIXELS
// (so the painted ground path matches the overlay line's thickness regardless of zoom).
layout(std430, set = 1, binding = 2) readonly buffer ProjectorParams { mat4 projViewProj; vec4 projMisc; };

layout(location = 0) out vec4 vColor;

// World half-width (in the XY ground plane) that projects to ~targetPx screen pixels at the given
// world point, measured against the MAIN camera (SceneData view/projection/screenHeight are still
// bound during the projector pass). m22 = projection[1][1] = vertical scale = 1/tan(fovY/2);
// a world length L at view-depth d covers ndc.y = m22*L/d, and the overlay offsets by 2*px/screenH.
float screenConstantHalfWidth(vec3 worldP, float targetPx)
{
    float depth = max(-(view * vec4(worldP, 1.0)).z, 0.01);
    float m22 = projection[1][1];
    return 2.0 * targetPx * depth / (m22 * screenHeight);
}

void main()
{
    int vid = gl_VertexIndex;
    int seg = vid / 6;
    int corner = vid % 6;
    int base = seg * 3;

    vec3 A = waypoints[base + 0].xyz;
    vec3 B = waypoints[base + 1].xyz;
    vColor = waypoints[base + 2];

    float targetPx = projMisc.x;
    float halfA = screenConstantHalfWidth(A, targetPx);
    float halfB = screenConstantHalfWidth(B, targetPx);

    // perpendicular to the segment within the world XY plane (the projection looks straight down Z)
    vec2 dxy = B.xy - A.xy;
    float len = length(dxy);
    vec2 dir = len > 1e-5 ? dxy / len : vec2(1.0, 0.0);
    vec2 perp = vec2(-dir.y, dir.x);

    vec3 p;
    if (corner == 0)      p = vec3(A.xy - perp * halfA, A.z);
    else if (corner == 1) p = vec3(A.xy + perp * halfA, A.z);
    else if (corner == 2) p = vec3(B.xy - perp * halfB, B.z);
    else if (corner == 3) p = vec3(B.xy - perp * halfB, B.z);
    else if (corner == 4) p = vec3(A.xy + perp * halfA, A.z);
    else                  p = vec3(B.xy + perp * halfB, B.z);

    gl_Position = projViewProj * vec4(p, 1.0);
}
