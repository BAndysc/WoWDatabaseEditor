#version 450
#include "../internalShaders/theengine.cginc"

// params.x = alpha multiplier for this pass (1.0 where the line is in front of geometry, <1.0
// where it is occluded - see WaypointRenderStage's two-pass draw).
layout(std430, set = 1, binding = 2) readonly buffer WaypointParams { vec4 params; };

layout(location = 0) in vec4 vColor;
layout(location = 0) out vec4 FragColor;

void main()
{
    FragColor = vColor;
    FragColor.a *= params.x;
}
