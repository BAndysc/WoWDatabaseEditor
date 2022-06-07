#version 330 core
#include "../internalShaders/theengine.cginc"
in vec2 TexCoord;
in vec4 Normal;
in vec3 WorldPos;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;

uniform vec4 top;
uniform vec4 middle;
uniform vec4 towardsHorizon;
uniform vec4 horizon;
uniform vec4 justAboveHorizon;
uniform vec4 sunColor;


uniform vec4 cloudsColor1;
uniform float timeOfDay; // 0 - midnight, 0.5 - noon, 1 - midnight
uniform float timeOfDayHalf; // 0 - noon, 0.5 - midnight, 1 - noon
uniform float cloudsDensity;

uniform sampler2D cloudsTex;

float remap(float value, float min1, float max1, float min2, float max2) 
{
    value = clamp(value, min1, max1);
    return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
}

vec3 AddGradient(vec4 from, vec4 to, float start, float end, float t)
{
    float use = step(start, t) * (1 - step(end, t));
    
    return use * mix(from.rgb, to.rgb, remap(t, start, end, 0, 1));
}

void main()
{
    // background gradient
    vec3 color = AddGradient(middle, top, 0.6, 1, TexCoord.y);
    color += AddGradient(towardsHorizon, middle, 0.5, 0.6, TexCoord.y);
    color += AddGradient(horizon, towardsHorizon, 0.45, 0.5, TexCoord.y);
    color += AddGradient(justAboveHorizon, horizon, 0.4, 0.45, TexCoord.y);
    color += AddGradient(justAboveHorizon, justAboveHorizon, 0, 0.40, TexCoord.y);
    
    // sun
    float sunStrength = distance(lightDir.xyz, Normal.xyz);   
    color = mix(color, sunColor.rgb, (step(sunStrength, 0.1)) * (smoothstep(0.45, 0.5, TexCoord.y)));
    

    // stars
    
    // is night? using timeOfDay we extract hours 0-6 and 20-24
    float isNight = 1 - smoothstep(0.4, 0.6, sin(timeOfDay * PI));
    
    vec3 normalizedWorldPos = normalize(WorldPos);
    float y = asin(normalizedWorldPos.x) / (3.1415 / 2);
    float x = atan(normalizedWorldPos.y, normalizedWorldPos.z) / (3.1415 / 2);
    vec2 uv = vec2(x, y);
    
    float noise = step(0.7, Noise(uv, 200));
    float starStrength, cellColor;
    Voronoi(uv + vec2(timeOfDayHalf / 10, 0), 130, 80, starStrength, cellColor);
    starStrength = isNight * pow(smoothstep(0.8, 1, 1 - starStrength), 2) * noise * (smoothstep(0.55, 0.6, TexCoord.y));// * (1 - smoothstep(0.75, 0.85, TexCoord.y));
    color = mix(color, vec3(1), starStrength);
        
    
    // clouds
    vec2 cloudCoord = WorldPos.xz / 2 / pow(abs(WorldPos.y), 0.1);
    //float cloud1 = texture(cloudsTex, vec2(cloudCoord.x * 0.5, cloudCoord.y * 2) + vec2(timeOfDay, timeOfDay)).r;
    //float cloud2 = texture(cloudsTex, vec2(cloudCoord.x * 1.2, cloudCoord.y / 3) + vec2(timeOfDay, -timeOfDay)).r;
    //float cloud = clamp(cloud1 * cloud2, 0, 1);
    float cloud = texture(cloudsTex, cloudCoord + vec2(timeOfDay, timeOfDay) / 14).r;
    float isMidnight = smoothstep(0, 0.4, sin(timeOfDay * PI));
    color = mix(color, cloudsColor1.rgb, cloudsDensity * isMidnight * pow(cloud, 3) * (smoothstep(0.52, 0.55, TexCoord.y))); // * (1 - smoothstep(0.75, 0.85, TexCoord.y)) 
    
    FragColor = vec4(color.rgb, 1);
    ObjectIndexOutputBuffer = uint(0);
}