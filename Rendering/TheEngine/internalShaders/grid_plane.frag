#version 330 core
#include "theengine.cginc"
in vec3 NearPoint;
in vec3 FarPoint;
out vec4 FragColor;

vec4 Grid(vec3 pos, float scale, bool drawAxis)
{
    vec2 coord = pos.xy * scale;
    vec2 derivative = fwidth(coord);

    float ff = sqrt(derivative.x*derivative.x+derivative.y*derivative.y);
    const float ff_min = 0;
    const float ff_max = 0.5;

    ff = clamp(ff, ff_min, ff_max);
    ff = (ff - ff_min) / (ff_max-ff_min);

    vec2 grid = abs(fract(coord - 0.5f) - 0.5f) / derivative;
    float minGrid = min(grid.x, grid.y);
    float minimumZ = min(derivative.y, 1.0f);
    float minimumX = min(derivative.x, 1.0f);

    vec4 color = vec4(0.3f, 0.3f, 0.3f, 1.0f - min(minGrid, 1.0f));

    // Y Axis
    if (pos.x > -0.1f * minimumX && pos.x < 0.1f * minimumX && drawAxis)
    {
        color.y = 1.0f;
    }

    // X Axis
    if (pos.y > -0.1f * minimumZ && pos.y < 0.1f * minimumZ && drawAxis)
    {
        color.x = 1.0f;
    }

    color.a *= 1 - ff;

    return color;
}


void main()
{
    float t = -NearPoint.z / (FarPoint.z - NearPoint.z);

    if (t < 0)
    {
        discard;
    }

    vec3 worldPos = NearPoint + t * (FarPoint - NearPoint);


    float distZ = abs(pow(cameraPos.z, 0.4));

    float fadeStart = 0;
    float fadeEnd = 10 * distZ;

    float distX = distance(worldPos.x, cameraPos.x);
    float distY = distance(worldPos.y, cameraPos.y);
    float dist = sqrt(distX * distX + distY * distY);

    float fading = clamp((fadeEnd - dist) / (fadeEnd - fadeStart), 0.0f, 1);//0.3f);

    FragColor = Grid(worldPos, 1.0f, true) + Grid(worldPos, 10.0f, true)/* + Grid(worldPos, 0.01f, true)*/;
    // FragColor.a *= fading;
    //     FragColor = vec4(NearPoint / 1000, 1);

}