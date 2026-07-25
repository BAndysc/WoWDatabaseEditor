#version 450
#extension GL_EXT_nonuniform_qualifier : require
// bindless: scene color + depth arrive as packed indices in the material data (texIndices).
layout(set = 2, binding = 0) uniform texture2D bindlessTextures[16384];
layout(set = 2, binding = 1) uniform sampler bindlessSamplers[128];
#define SAMPLE_BINDLESS(idx, uv) texture(sampler2D(bindlessTextures[nonuniformEXT(uint(idx) & 0xFFFFFu)], bindlessSamplers[nonuniformEXT(uint(idx) >> 20u)]), uv)

layout(location = 0) out vec4 FragColor;
layout(location = 0) in vec2 TexCoords;

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    mat4 invProjection;
    mat4 projection;
    vec4 aoParams;        // x=radius, y=intensity, z=power, w=bias
    vec4 misc;            // x,y=min/max dist, z=enabled, w=unused
    vec4 samples[16];     // Precomputed hemisphere samples from CPU
    ivec4 texIndices;     // x=_MainTex bindless index, y=_DepthTex bindless index
};

#define _MainTexIndex texIndices.x
#define _DepthTexIndex texIndices.y

const int SAMPLES = 16;

// Interleaved Gradient Noise: Extremely cheap, trig-free spatial noise
float InterleavedGradientNoise(vec2 uv)
{
    vec3 magic = vec3(0.06711056, 0.00583715, 52.9829189);
    return fract(magic.z * fract(dot(uv, magic.xy)));
}

vec3 reconstructViewPos(vec2 uv, float d)
{
    // native Vulkan depth: stored d is already the projection's ndc z in [0,1].
    // uv is the top-down screen uv (v=0 at top), so ndc y = 1 - 2*uv.y.
    vec4 ndc = vec4(uv.x * 2.0 - 1.0, 1.0 - 2.0 * uv.y, d, 1.0);
    vec4 view = invProjection * ndc;
    return view.xyz / view.w;
}

void main()
{
    float depth = SAMPLE_BINDLESS(_DepthTexIndex, TexCoords).r;

    if (misc.z < 0.5 || depth >= 1.0)
    {
        FragColor = SAMPLE_BINDLESS(_MainTexIndex, TexCoords);
        return;
    }

    float radius = aoParams.x;
    float intensity = aoParams.y;
    float power = aoParams.z;
    float baseBias = aoParams.w;

    vec3 fragPos = reconstructViewPos(TexCoords, depth);

    vec3 normal = normalize(cross(dFdx(fragPos), dFdy(fragPos)));
    if (normal.z < 0.0) normal = -normal;

    float randomAngle = InterleavedGradientNoise(gl_FragCoord.xy) * 3.14159265 * 2.0;
    vec3 randomVec = vec3(cos(randomAngle), sin(randomAngle), 0.0);

    vec3 tangent = normalize(randomVec - normal * dot(randomVec, normal));
    vec3 bitangent = cross(normal, tangent);
    mat3 TBN = mat3(tangent, bitangent, normal);

    // Calculate how glancing the camera angle is (0.0 = edge-on, 1.0 = straight-on)
    vec3 viewDir = normalize(-fragPos);
    float NdotV = max(dot(normal, viewDir), 0.0);
    // Increase the bias drastically at glancing angles to prevent black walls
    float scaledBias = baseBias * mix(5.0, 1.0, NdotV);

    float occlusion = 0.0;

    for (int i = 0; i < SAMPLES; ++i)
    {
        // Push the center of the hemisphere slightly out along the normal
        vec3 samplePos = fragPos + normal * scaledBias + (TBN * samples[i].xyz) * radius;

        vec4 offset = projection * vec4(samplePos, 1.0);
        offset.xyz /= offset.w;
        // match the top-down screen-uv convention (v=0 at top): uv.y = 0.5 - 0.5*ndc.y
        vec2 sUV = vec2(offset.x * 0.5 + 0.5, 0.5 - offset.y * 0.5);

        if (sUV.x < 0.0 || sUV.x > 1.0 || sUV.y < 0.0 || sUV.y > 1.0) continue;

        float sd = SAMPLE_BINDLESS(_DepthTexIndex, sUV).r;
        float sampleSurfaceZ = reconstructViewPos(sUV, sd).z;

        float rangeCheck = smoothstep(0.0, 1.0, radius / max(0.0001, abs(fragPos.z - sampleSurfaceZ)));

        occlusion += (sampleSurfaceZ >= samplePos.z ? 1.0 : 0.0) * rangeCheck;
    }

    float ao = 1.0 - (occlusion / float(SAMPLES)) * intensity;
    ao = pow(clamp(ao, 0.0, 1.0), power);

    // View Z is negative, so we use -fragPos.z to get positive distance.
    float distanceFade = smoothstep(misc.x, misc.y, -fragPos.z);
    ao = mix(ao, 1.0, distanceFade);

    vec4 color = SAMPLE_BINDLESS(_MainTexIndex, TexCoords);
    FragColor = vec4(color.rgb * ao, color.a);
}