#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 1) in vec2 TexCoord;
#ifndef DEPTH_PASS
layout(location = 0) in vec4 Color;
layout(location = 2) in vec2 TexCoord2;
layout(location = 3) in vec4 WorldPos;
layout(location = 4) in vec4 SplatId;
layout(location = 5) in vec3 Normal;
layout(location = 6) flat in int instanceID;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;
#endif

struct MaterialData
{
    float alphaTest;
    float notSupported;
    int shader_id;
    int unlit;
    int brightAtNight;
    int interior;
    int translucent;
    int texture1Index;
    int texture2Index;
    int wmo_pad1;
    int wmo_pad2;
    int wmo_pad3;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

int orInt(int a, int b)
{
    return min(1, a + b);
}

vec3 min3(vec3 a, vec3 b)
{
    return vec3(min(a.x, b.x), min(a.y, b.y), min(a.z, b.z));
}

vec3 max3(vec3 a, vec3 b)
{
    return vec3(max(a.x, b.x), max(a.y, b.y), max(a.z, b.z));
}

#ifdef DEPTH_PASS

void main()
{
    // depth/shadow pass: cutout discard only, no color output (must match the forward cutout)
    if (SAMPLE_BINDLESS(M(texture1Index), TexCoord.xy).a < M(alphaTest))
        discard;
    float makeTranslucent = mix(float(1), float(int((gl_FragCoord.x + gl_FragCoord.y)) % 2), float(M(translucent)));
    if (makeTranslucent < 0.5)
        discard;
}

#else

void main()
{
    PIXEL_SETUP_INSTANCING(instanceID);

    vec4 tex = SAMPLE_BINDLESS(M(texture1Index), TexCoord.xy);
    vec4 tex_2 = SAMPLE_BINDLESS(M(texture2Index), TexCoord.xy);

    if (tex.a < M(alphaTest))
        discard;

    float makeTranslucent = mix(float(1), float(int((gl_FragCoord.x + gl_FragCoord.y)) % 2), float(M(translucent)));
    if (makeTranslucent < 0.5)
        discard;

    vec3 diffuse;

    // see: https://github.com/Deamon87/WebWowViewerCpp/blob/master/wowViewerLib/src/glsl/wmoShader.glsl
    if(M(shader_id) == 3) // Env
    {
        vec3 env = tex_2.rgb * tex.rgb;
        diffuse = tex.rgb + env;
    }
    else if(M(shader_id) == 5) // EnvMetal
    {
        vec3 env = tex_2.rgb * tex.rgb * tex.a;
        diffuse = tex.rgb + env;
    }
    else if(M(shader_id) == 6) // TwoLayerDiffuse
    {
        vec3 layer2 = mix(tex.rgb, tex_2.rgb, tex_2.a);
        diffuse = mix(layer2, tex.rgb, Color.a);
    }
    else // default shader, used for shader_id 0,1,2,4 (Diffuse, Specular, Metal, Opaque)
    {
        diffuse = tex.xyz;
    }

    uint decalPickId;
    diffuse = ApplyDecals(diffuse, Normal.xyz, WorldPos.xyz, decalPickId);

    vec3 lighted = lighting(diffuse, Normal.xyz, WorldPos.xyz);
    vec3 interiorLight = diffuse * max3(Color.rgb, vec3(0.3));
    vec3 finalColor = mix(lighted, interiorLight, float(M(interior)));
    finalColor = mix(finalColor, diffuse, float(orInt(M(brightAtNight), M(unlit))));

    FragColor = ApplyFog(vec4(finalColor, 1.0), WorldPos.xyz);
    ObjectIndexOutputBuffer = decalPickId != 0u ? decalPickId : objectIndex;
}

#endif
