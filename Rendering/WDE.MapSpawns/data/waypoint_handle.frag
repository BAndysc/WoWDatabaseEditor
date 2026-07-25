#version 450
#include "../internalShaders/theengine.cginc"

// params.x = alpha multiplier for this pass (1.0 where the dot is in front of geometry, <1.0 where
// it is occluded - see WaypointRenderStage's two-pass draw).
layout(std430, set = 1, binding = 2) readonly buffer HandleParams { vec4 params; };

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vOff;
layout(location = 0) out vec4 FragColor;

void main()
{
    float r = length(vOff);
    if (r > 1.0)
        discard;
    // soft dark rim so the dots read clearly against any background
    float rim = smoothstep(0.75, 1.0, r);
    FragColor = vec4(mix(vColor.rgb, vec3(0.0), rim), vColor.a * params.x);
}
