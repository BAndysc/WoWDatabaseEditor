#version 450
#include "theengine.cginc"

layout(location = 0) in vec3 NearPoint;
layout(location = 1) in vec3 FarPoint;
layout(location = 0) out vec4 FragColor;

vec4 Grid(vec3 pos, float scale, bool drawAxis)
{
    vec2 coord = pos.xy * scale;
    vec2 derivative = fwidth(coord);

    float ff = sqrt(derivative.x*derivative.x+derivative.y*derivative.y);
    const float ff_min = 0.0;
    const float ff_max = 0.5;

    ff = clamp(ff, ff_min, ff_max);
    ff = (ff - ff_min) / (ff_max-ff_min);

    vec2 grid = abs(fract(coord - 0.5) - 0.5) / derivative;
    float minGrid = min(grid.x, grid.y);
    float minimumZ = min(derivative.y, 1.0);
    float minimumX = min(derivative.x, 1.0);

    vec4 color = vec4(0.3, 0.3, 0.3, 1.0 - min(minGrid, 1.0));

    // Y Axis
    if (pos.x > -0.1 * minimumX && pos.x < 0.1 * minimumX && drawAxis)
    {
        color.y = 1.0;
    }

    // X Axis
    if (pos.y > -0.1 * minimumZ && pos.y < 0.1 * minimumZ && drawAxis)
    {
        color.x = 1.0;
    }

    color.a *= 1.0 - ff;

    return color;
}


void main()
{
    float t = -NearPoint.z / (FarPoint.z - NearPoint.z);

    if (t < 0.0)
    {
        discard;
    }

    vec3 worldPos = NearPoint + t * (FarPoint - NearPoint);

    vec4 clipPos = projection * view * vec4(worldPos, 1.0);
    // native Vulkan depth: stored value is the projection's ndc z in [0,1] directly
    gl_FragDepth = clipPos.z / clipPos.w;

    float distZ = abs(pow(cameraPos.z, 0.4));

    float fadeStart = 0.0;
    float fadeEnd = 10.0 * distZ;

    float distX = distance(worldPos.x, cameraPos.x);
    float distY = distance(worldPos.y, cameraPos.y);
    float dist = sqrt(distX * distX + distY * distY);

    float fading = clamp((fadeEnd - dist) / (fadeEnd - fadeStart), 0.0, 1.0);

    FragColor = Grid(worldPos, 1.0, true) + Grid(worldPos, 10.0, true) + Grid(worldPos, 100.0, true);
}
