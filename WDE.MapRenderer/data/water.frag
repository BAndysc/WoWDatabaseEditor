#version 330 core
#include "../internalShaders/theengine.cginc"

in vec4 WorldPos;
in vec3 ScreenPos;
out vec4 FragColor;

uniform vec4 color;
uniform vec4 deepColor;
uniform vec4 shallowColor;

uniform sampler2D _SceneColor;
uniform sampler2D _DepthTexture;
uniform sampler2D _WaterTexture;

vec3 WorldPosFromDepth(float depth, vec2 TexCoord) {
    float z = depth * 2.0 - 1.0;

    vec4 clipSpacePosition = vec4(TexCoord * 2.0 - 1.0, z, 1.0);
    vec4 viewSpacePosition = projectionInv * clipSpacePosition;

    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    vec4 worldSpacePosition = viewInv * viewSpacePosition;

    return worldSpacePosition.xyz;
}

float LinearEyeDepth(float d)
{
    return zNear * zFar / (zFar + d * (zNear - zFar));
}

float Linear01Depth(float d)
{
    return (LinearEyeDepth(d) - zNear) / (zFar - zNear);
}

vec3 NormalBlend(vec3 A, vec3 B)
{
    return normalize(vec3(A.rg + B.rg, A.b * B.b));
}

void main()
{   
    //
    vec3 offset = texture(_WaterTexture, WorldPos.xy / 100 + vec2(time, time) / 100000).xyz;
    vec3 offset2 = texture(_WaterTexture, WorldPos.xy / 100 - vec2(time, time) / 100000).xyz;
    offset = NormalBlend((offset - 0.5) * 2, (offset2 - 0.5) * 2);

    vec2 screenPos = vec2(gl_FragCoord.x / screenWidth, gl_FragCoord.y / screenHeight);
    
    float depth = texture(_DepthTexture, screenPos).r;
    vec3 terrainWorldPos = WorldPosFromDepth(depth, screenPos);
    vec3 originalTerrainWorldPos = terrainWorldPos;
    
    // 1 deep
    // 0 shallow
    float diffHeight = clamp((WorldPos.z - originalTerrainWorldPos.z) / 30, 0, 1);

    float foamFactor = 1 - clamp((WorldPos.z - originalTerrainWorldPos.z) / (0.2 * length(offset)), 0, 1);
    float foamFactor2 = foamFactor * offset.z;

    float cameraIsMedium = clamp(cameraPos.z / 50, 0, 1);
    float cameraIsFar = clamp(cameraPos.z / 500, 0, 1);
    vec3 normalVeryNear = texture(_WaterTexture, WorldPos.xy / 3).xyz * 2 - 1;
    vec3 normal = texture(_WaterTexture, WorldPos.xy / 30).xyz * 2 - 1;
    vec3 normalFar = texture(_WaterTexture, WorldPos.xy / 100).xyz * 2 - 1;
    normal = lerp(lerp(normalVeryNear, normal, cameraIsMedium), normalFar, cameraIsFar);
    vec3 viewDir = normalize(cameraPos.xyz - WorldPos.xyz);
    vec3 reflectDir = reflect(lightDir.xyz, normal);  
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 8);

    vec4 waterColor = lerp(shallowColor, deepColor, diffHeight);
    vec4 foamColor = vec4(1, 1, 1, 0.8 * foamFactor2);

    waterColor = waterColor + vec4(1, 1, 1, 0) * spec;

    waterColor = lerp(waterColor, foamColor, foamFactor);
    waterColor.a = waterColor.a * (1-foamFactor);


    float distanceToCamera = distance(cameraPos.xyz, terrainWorldPos.xyz);
    float depthEffectOnOffset = 1 - clamp(distanceToCamera/50, 0, 1);
    if (terrainWorldPos.z < WorldPos.z)
        screenPos = screenPos + vec2(offset.x, 0) * 0.02 * foamFactor;
    depth = texture(_DepthTexture, screenPos).r;
    terrainWorldPos = WorldPosFromDepth(depth, screenPos);
    vec4 sceneColor = texture(_SceneColor, screenPos);


    FragColor = color * 0.0001 + vec4(sceneColor.xyz * (1-waterColor.a) + waterColor.xyz * waterColor.a, 1);
}