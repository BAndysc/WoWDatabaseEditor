#version 330 core
#include "theengine.cginc"

out vec3 NearPoint;
out vec3 FarPoint;

vec3 UnprojectPoint(vec2 xy, float z)
{
    vec4 clipSpacePosition = vec4(xy, z, 1.0);
    vec4 viewSpacePosition = projectionInv * clipSpacePosition;

    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    vec4 worldSpacePosition = viewInv * viewSpacePosition;

    return worldSpacePosition.xyz;
}

void main()
{
    gl_Position = vec4(position.x, position.y, position.z, 1.0);
    NearPoint = UnprojectPoint(position.xy, 3);
    FarPoint = UnprojectPoint(position.xy, 0);
}