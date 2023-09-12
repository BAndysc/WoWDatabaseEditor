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

void main()
{
    //
    vec3 offset = texture(_WaterTexture, WorldPos.xy / 100 + vec2(time, time) / 100000).xyz;
    vec3 offset2 = texture(_WaterTexture, WorldPos.xy / 100 - vec2(time, time) / 100000).xyz;
    offset = NormalBlend((offset - 0.5) * 2, (offset2 - 0.5) * 2);

    vec2 screenPos = vec2(gl_FragCoord.x / screenWidth, gl_FragCoord.y / screenHeight);

    float depth = texture(_DepthTexture, screenPos).r;
    float originalDepth = depth;
    vec3 terrainWorldPos = WorldPosFromDepth(depth, screenPos);
    vec3 originalTerrainWorldPos = terrainWorldPos;

    float distanceToCamera = distance(WorldPos.xyz, cameraPos.xyz);
    float cameraDistanceFactor = 1 - clamp(distanceToCamera, 0, 300) / 300;

    float displacementStrength = clamp(abs(WorldPos.z - originalTerrainWorldPos.z) / 2, 0, 1);

    float foamFactor = 1 - clamp(abs(WorldPos.z - originalTerrainWorldPos.z) / (0.5 * (Noise(offset.xy, 2) + 0.5)), 0, 1);
    foamFactor *= cameraDistanceFactor;
    //foamFactor = step(0.5, foamFactor);
    float foamFactor2 = 0;//foamFactor * offset.z * cameraDistanceFactor;

    float timeDivider = 200000;

    float cameraIsMedium = clamp(abs(cameraPos.z - WorldPos.z) / 30, 0, 1);
    float cameraIsFar = clamp(abs(cameraPos.z - WorldPos.z) / 500, 0, 1);

    vec3 normalVeryNear = texture(_WaterTexture, WorldPos.xy / 3+ vec2(time, time) / timeDivider).xyz * 2 - 1;
    vec3 normal = texture(_WaterTexture, WorldPos.xy / 30+ vec2(time, time) / timeDivider).xyz * 2 - 1;
    vec3 normalFar = texture(_WaterTexture, WorldPos.xy / 100+ vec2(time, time) / timeDivider).xyz * 2 - 1;

    vec3 normalVeryNear2 = texture(_WaterTexture, WorldPos.xy / 3 - vec2(time, time) / timeDivider).xyz * 2 - 1;
    vec3 normal2 = texture(_WaterTexture, WorldPos.xy / 30 - vec2(time, time) / timeDivider).xyz * 2 - 1;
    vec3 normalFar2 = texture(_WaterTexture, WorldPos.xy / 100 - vec2(time, time) / timeDivider).xyz * 2 - 1;

    normalVeryNear = NormalBlend(normalVeryNear, normalVeryNear2);
    normal = NormalBlend(normal, normal2);
    normalFar2 = NormalBlend(normalFar2, normalFar2);

    normal = lerp(lerp(normalVeryNear, normal, cameraIsMedium), normalFar, cameraIsFar);
    vec3 viewDir = normalize(cameraPos.xyz - WorldPos.xyz);
    vec3 reflectDir = reflect(lightDir.xyz, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 16);

    // displacement

    vec2 origScreenPos = screenPos;
    screenPos = screenPos + vec2(offset.x, 0) * 0.05 * displacementStrength;
    depth = texture(_DepthTexture, screenPos).r;
    terrainWorldPos = WorldPosFromDepth(depth, screenPos);
    if (gl_FragCoord.z > depth)
    screenPos = origScreenPos;
    vec4 sceneColor = texture(_SceneColor, screenPos);

    // 1 deep
    // 0 shallow
    float diffHeight = clamp((WorldPos.z - terrainWorldPos.z) / 30, 0, 1);

    vec4 waterColor = lerp(vec4(shallowColor.xyz, 0.5), deepColor, diffHeight);
    vec4 foamColor = vec4(2, 2, 2, 0.5);

    waterColor = waterColor + vec4(1, 1, 1, 0) * spec;

    //waterColor = lerp(waterColor, foamColor, foamFactor);
    waterColor.a = (1-foamFactor) * waterColor.a;//0.3;//waterColor.a * (1-foamFactor);


    FragColor = color * 0.0001 + vec4(sceneColor.xyz * (1-waterColor.a) + waterColor.xyz * waterColor.a, 1);
    FragColor = ApplyFog(FragColor, WorldPos.xyz);
}