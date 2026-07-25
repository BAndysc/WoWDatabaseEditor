#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) in vec2 TexCoord;
layout(location = 1) in vec3 WorldPos;
layout(location = 2) in vec4 Normal;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    vec4 top;
    vec4 middle;
    vec4 towardsHorizon;
    vec4 horizon;
    vec4 justAboveHorizon;
    vec4 sunColor;
    vec4 cloudsColor1;
    float timeOfDay;
    float timeOfDayHalf;
    float cloudsDensity;
    int cloudsTexIndex; // bindless slot of the clouds noise texture
};

float remap(float value, float min1, float max1, float min2, float max2)
{
    value = clamp(value, min1, max1);
    return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
}

vec3 AddGradient(vec4 from, vec4 to, float start, float end, float t)
{
    float use = step(start, t) * (1.0 - step(end, t));

    return use * mix(from.rgb, to.rgb, remap(t, start, end, 0.0, 1.0));
}

void main()
{
    // background gradient
    vec3 color = AddGradient(middle, top, 0.6, 1.0, TexCoord.y);
    color += AddGradient(towardsHorizon, middle, 0.5, 0.6, TexCoord.y);
    color += AddGradient(horizon, towardsHorizon, 0.45, 0.5, TexCoord.y);
    color += AddGradient(justAboveHorizon, horizon, 0.4, 0.45, TexCoord.y);
    color += AddGradient(justAboveHorizon, justAboveHorizon, 0.0, 0.40, TexCoord.y);

    // sun
    float sunStrength = distance(lightDir.xyz, Normal.xyz);
    color = mix(color, sunColor.rgb, (step(sunStrength, 0.1)) * (smoothstep(0.45, 0.5, TexCoord.y)));

    // stars
    float isNight = 1.0 - smoothstep(0.4, 0.6, sin(timeOfDay * PI));

    vec3 normalizedWorldPos = normalize(WorldPos);
    float y = asin(normalizedWorldPos.x) / (3.1415 / 2.0);
    float x = atan(normalizedWorldPos.y, normalizedWorldPos.z) / (3.1415 / 2.0);
    vec2 uv = vec2(x, y);

    float noise = step(0.7, Noise(uv, 200.0));
    float starStrength, cellColor;
    Voronoi(uv + vec2(timeOfDayHalf / 10.0, 0.0), 130.0, 80.0, starStrength, cellColor);
    starStrength = isNight * pow(smoothstep(0.8, 1.0, 1.0 - starStrength), 2.0) * noise * (smoothstep(0.55, 0.6, TexCoord.y));
    color = mix(color, vec3(1.0), starStrength);

    // clouds
    vec2 cloudCoord = WorldPos.xz / 2.0 / pow(abs(WorldPos.y), 0.1);
    float cloud = SAMPLE_BINDLESS(cloudsTexIndex, cloudCoord + vec2(timeOfDay, timeOfDay) / 14.0).r;
    float isMidnight = smoothstep(0.0, 0.4, sin(timeOfDay * PI));
    color = mix(color, cloudsColor1.rgb, cloudsDensity * isMidnight * pow(cloud, 3.0) * (smoothstep(0.52, 0.55, TexCoord.y)));

    FragColor = vec4(color.rgb, 1.0);
    ObjectIndexOutputBuffer = uint(0);
}
