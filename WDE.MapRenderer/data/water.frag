#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 0) in vec3 ScreenPos;
layout(location = 1) in vec4 WorldPos;
layout(location = 0) out vec4 FragColor;

struct MaterialData
{
    vec4 color;
    vec4 deepColor;
    vec4 shallowColor;
    int waterTexIndex;   // bindless slot of the water normal/offset texture
    int sceneColorIndex; // bindless slot of the scene opaque-color RT
    int depthTexIndex;   // bindless slot of the scene depth RT
    int pad;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

vec3 WorldPosFromDepth(float depth, vec2 TexCoord) {
    // native Vulkan depth: stored value is the projection's ndc z in [0,1]; TexCoord is the
    // top-down screen uv (v=0 at top), so ndc y = 1 - 2*TexCoord.y.
    float z = depth;

    vec4 clipSpacePosition = vec4(TexCoord.x * 2.0 - 1.0, 1.0 - 2.0 * TexCoord.y, z, 1.0);
    vec4 viewSpacePosition = projectionInv * clipSpacePosition;

    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    vec4 worldSpacePosition = viewInv * viewSpacePosition;

    return worldSpacePosition.xyz;
}

void main()
{
    vec3 offset = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 100.0 + vec2(time, time) / 100000.0).xyz;
    vec3 offset2 = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 100.0 - vec2(time, time) / 100000.0).xyz;
    offset = NormalBlend((offset - 0.5) * 2.0, (offset2 - 0.5) * 2.0);

    vec2 screenPos = vec2(gl_FragCoord.x / screenWidth, gl_FragCoord.y / screenHeight);

    float depth = SAMPLE_BINDLESS(M(depthTexIndex), screenPos).r;
    float originalDepth = depth;
    vec3 terrainWorldPos = WorldPosFromDepth(depth, screenPos);
    vec3 originalTerrainWorldPos = terrainWorldPos;

    float distanceToCamera = distance(WorldPos.xyz, cameraPos.xyz);
    float cameraDistanceFactor = 1.0 - clamp(distanceToCamera, 0.0, 300.0) / 300.0;

    float displacementStrength = clamp(abs(WorldPos.z - originalTerrainWorldPos.z) / 2.0, 0.0, 1.0);

    float foamFactor = 1.0 - clamp(abs(WorldPos.z - originalTerrainWorldPos.z) / (0.5 * (Noise(offset.xy, 2.0) + 0.5)), 0.0, 1.0);
    foamFactor *= cameraDistanceFactor;
    float foamFactor2 = 0.0;

    float timeDivider = 200000.0;

    float cameraIsMedium = clamp(abs(cameraPos.z - WorldPos.z) / 30.0, 0.0, 1.0);
    float cameraIsFar = clamp(abs(cameraPos.z - WorldPos.z) / 500.0, 0.0, 1.0);

    vec3 normalVeryNear = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 3.0 + vec2(time, time) / timeDivider).xyz * 2.0 - 1.0;
    vec3 normal = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 30.0 + vec2(time, time) / timeDivider).xyz * 2.0 - 1.0;
    vec3 normalFar = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 100.0 + vec2(time, time) / timeDivider).xyz * 2.0 - 1.0;

    vec3 normalVeryNear2 = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 3.0 - vec2(time, time) / timeDivider).xyz * 2.0 - 1.0;
    vec3 normal2 = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 30.0 - vec2(time, time) / timeDivider).xyz * 2.0 - 1.0;
    vec3 normalFar2 = SAMPLE_BINDLESS(M(waterTexIndex), WorldPos.xy / 100.0 - vec2(time, time) / timeDivider).xyz * 2.0 - 1.0;

    normalVeryNear = NormalBlend(normalVeryNear, normalVeryNear2);
    normal = NormalBlend(normal, normal2);
    normalFar2 = NormalBlend(normalFar2, normalFar2);

    normal = lerp(lerp(normalVeryNear, normal, cameraIsMedium), normalFar, cameraIsFar);
    vec3 viewDir = normalize(cameraPos.xyz - WorldPos.xyz);
    vec3 reflectDir = reflect(lightDir.xyz, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 16.0);

    vec2 origScreenPos = screenPos;
    screenPos = screenPos + vec2(offset.x, 0.0) * 0.05 * displacementStrength;
    depth = SAMPLE_BINDLESS(M(depthTexIndex), screenPos).r;
    terrainWorldPos = WorldPosFromDepth(depth, screenPos);
    if (gl_FragCoord.z > depth)
        screenPos = origScreenPos;
    vec4 sceneColor = SAMPLE_BINDLESS(M(sceneColorIndex), screenPos);

    float diffHeight = clamp((WorldPos.z - terrainWorldPos.z) / 30.0, 0.0, 1.0);

    vec4 waterColor = lerp(vec4(M(shallowColor).xyz, 0.5), M(deepColor), diffHeight);
    vec4 foamColor = vec4(2.0, 2.0, 2.0, 0.5);

    waterColor = waterColor + vec4(1.0, 1.0, 1.0, 0.0) * spec;

    waterColor.a = (1.0-foamFactor) * waterColor.a;

    FragColor = M(color) * 0.0001 + vec4(sceneColor.xyz * (1.0-waterColor.a) + waterColor.xyz * waterColor.a, 1.0);
    FragColor = ApplyFog(FragColor, WorldPos.xyz);
}
