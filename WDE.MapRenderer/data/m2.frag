#version 450
#include "../internalShaders/theengine.cginc"

layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec2 TexCoord2;
layout(location = 6) in vec4 VertexColor;
#ifndef DEPTH_PASS
layout(location = 0) in vec4 Color;
layout(location = 3) in vec4 WorldPos;
layout(location = 4) in vec4 SplatId;
layout(location = 5) in vec3 Normal;
layout(location = 7) flat in int instanceID;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;
#endif

struct MaterialData
{
    float alphaTest;
    float notSupported;
    float highlight;
    int unlit;
    int pixel_shader;
    int translucent;
    int texture1Index;
    int texture2Index;
    int texture3Index;
    int m2_pad1;
    int m2_pad2;
    int m2_pad3;
    vec4 mesh_color;
};
layout(std430, set = 1, binding = 0) readonly buffer MaterialDataArray { MaterialData materials[]; };
#define M(field) materials[MATERIAL_INDEX].field

vec4 ShadeCModel(int pixelId, vec4 tex1, vec4 tex2, vec4 tex3)
{
    vec4 result = vec4(0.0, 0.0, 0.0, 1.0) + M(mesh_color) * 0.00001;
    vec4 diffuseColor = vec4(1.0, 1.0, 1.0, 1.0);
    vec4 pixelColor1 = vec4(1.0, 1.0, 1.0, 1.0);
    vec3 specular = vec3(0.0, 0.0, 0.0);
    bool canDiscard = false;

    // based on Deamon's code, thank you!
    // https://github.com/Deamon87/WebWowViewerCpp/blob/master/wowViewerLib/shaders/glsl/vulkan/m2Shader.frag
    if (pixelId == 0) { //Combiners_Opaque
        result.rgb = diffuseColor.rgb * tex1.rgb;
    } else if (pixelId == 1) { //Combiners_Mod
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = tex1.a;
        canDiscard = true;
    } else if (pixelId == 2) { //Combiners_Opaque_Mod
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
        result.a = tex2.a;
        canDiscard = true;
    } else if (pixelId == 3) { //Combiners_Opaque_Mod2x
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb * 2.0;
        result.a = tex2.a * 2.0;
        canDiscard = true;
    } else if (pixelId == 4) { //Combiners_Opaque_Mod2xNA
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb * 2.0;
    } else if (pixelId == 5) { //Combiners_Opaque_Opaque
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
    }
    else if (pixelId == 6) { //Combiners_Mod_Mod
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
        result.a = tex1.a * tex2.a;
        canDiscard = true;
    } else if (pixelId == 7) { //Combiners_Mod_Mod2x
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb * 2.0;
        result.a = tex1.a * tex2.a * 2.0;
        canDiscard = true;
    } else if (pixelId == 8) { //Combiners_Mod_Add
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = tex1.a + tex2.a;
        canDiscard = true;
    } else if (pixelId == 9) { //Combiners_Mod_Mod2xNA
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb * 2.0;
        result.a = tex1.a;
        canDiscard = true;
    } else if (pixelId == 10) { //Combiners_Mod_AddNA
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = tex1.a;
        canDiscard = true;
    } else if (pixelId == 11) { //Combiners_Mod_Opaque
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
        result.a = tex1.a;
        canDiscard = true;
    } else if (pixelId == 12) { //Combiners_Opaque_Mod2xNA_Alpha
        result.rgb = diffuseColor.rgb * mix(tex1.rgb * tex2.rgb * 2.0, tex1.rgb, vec3(tex1.a));
    } else if (pixelId == 13) { //Combiners_Opaque_AddAlpha
        result.rgb = diffuseColor.rgb * tex1.rgb;
    } else if (pixelId == 14) { //Combiners_Opaque_AddAlpha_Alpha
        result.rgb = diffuseColor.rgb * tex1.rgb;
    } else if (pixelId == 15) { //Combiners_Opaque_Mod2xNA_Alpha_Add
        result.rgb = diffuseColor.rgb * mix(tex1.rgb * tex2.rgb * 2.0, tex1.rgb, vec3(tex1.a));
    } else if (pixelId == 16) { //Combiners_Mod_AddAlpha
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = tex1.a;
        canDiscard = true;
    } else if (pixelId == 17) { //Combiners_Mod_AddAlpha_Alpha
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = tex1.a + tex2.a * (0.3 * tex2.r + 0.59 * tex2.g + 0.11 * tex2.b);
        canDiscard = true;
    } else if (pixelId == 18) { //Combiners_Opaque_Alpha_Alpha
        result.rgb = diffuseColor.rgb * mix(mix(tex1.rgb, tex2.rgb, vec3(tex2.a)), tex1.rgb, vec3(tex1.a));
    } else if (pixelId == 19) { //Combiners_Opaque_Mod2xNA_Alpha_3s
        result.rgb = diffuseColor.rgb * mix(tex1.rgb * tex2.rgb * 2.0, tex3.rgb, vec3(tex3.a));
    } else if (pixelId == 20) { //Combiners_Opaque_AddAlpha_Wgt
        result.rgb = diffuseColor.rgb * tex1.rgb;
    } else if (pixelId == 21) { //Combiners_Mod_Add_Alpha
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = tex1.a + tex2.a;
        canDiscard = true;
    } else if (pixelId == 22) { //Combiners_Opaque_ModNA_Alpha
        result.rgb = diffuseColor.rgb * mix(tex1.rgb * tex2.rgb, tex1.rgb, vec3(tex1.a));
    }
    else if (pixelId == 33) //Combiners_Mod_Depth
    {
        result.rgb = diffuseColor.rgb * 2.0 * tex1.rgb;
        result.a = tex1.a;
    }
    else if (pixelId == 50)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 51) // CModelPixelShaderID::Opaque_Opaque
    {
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 52) // CModelPixelShaderID::Opaque_Mod
    {
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
        result.a = tex2.a * diffuseColor.a;
    }
    else if (pixelId == 53) // CModelPixelShaderID::Opaque_Mod2x
    {
        result.rgb = diffuseColor.rgb * 2.0 * tex1.rgb * tex2.rgb;
        result.a = diffuseColor.a * 2.0 * tex2.a;
    }
    else if (pixelId == 54) // CModelPixelShaderID::Opaque_Mod2xNA
    {
        result.rgb = diffuseColor.rgb * 2.0 * tex1.rgb * tex2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 55) // CModelPixelShaderID::Opaque_Add)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb + tex2.rgb;
        result.a = diffuseColor.a + tex1.a;
    }
    else if (pixelId == 56) // CModelPixelShaderID::Opaque_AddNA)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb + tex2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 57) //CModelPixelShaderID::Opaque_AddAlpha)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = diffuseColor.a;
        specular = tex2.rgb * tex2.a;
    }
    else if (pixelId == 58) //CModelPixelShaderID::Opaque_AddAlpha_Alpha)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = diffuseColor.a;
        specular = tex2.rgb * tex2.a * (1.0 - tex1.a);
    }
    else if (pixelId == 59) //CModelPixelShaderID::Opaque_Mod2xNA_Alpha)
    {
        result.rgb = diffuseColor.rgb * mix(tex1.rgb * tex2.rgb * 2.0, tex1.rgb, tex1.aaa);
        result.a = diffuseColor.a;
    }
    else if (pixelId == 60) //CModelPixelShaderID::Mod)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = diffuseColor.a * tex1.a;
    }
    else if (pixelId == 61) //CModelPixelShaderID::Mod_Opaque)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
        result.a = diffuseColor.a * tex1.a;
    }
    else if (pixelId == 62) //CModelPixelShaderID::Mod_Mod)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb * tex2.rgb;
        result.a = diffuseColor.a * tex1.a * tex2.a;
    }
    else if (pixelId == 63) //CModelPixelShaderID::Mod_Mod2x)
    {
        result.rgb = diffuseColor.rgb * 2.0 * tex1.rgb * tex2.rgb;
        result.a = diffuseColor.a * 2.0 * tex1.a * tex2.a;
    }
    else if (pixelId == 64) //CModelPixelShaderID::Mod_Mod2xNA)
    {
        result.rgb = diffuseColor.rgb * 2.0 * tex1.rgb * tex2.rgb;
        result.a = tex1.a * diffuseColor.a;
    }
    else if (pixelId == 65) //CModelPixelShaderID::Mod_Add)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = diffuseColor.a * (tex1.a + tex2.a);
        specular = tex2.rgb;
    }
    else if (pixelId == 67) //CModelPixelShaderID::Mod_AddNA)
    {
        result.rgb = diffuseColor.rgb * tex1.rgb;
        result.a = tex1.a * diffuseColor.a;
        specular = tex2.rgb;
    }
    else if (pixelId == 68) //CModelPixelShaderID::Mod2x)
    {
        result.rgb = diffuseColor.rgb * 2.0 * tex1.rgb;
        result.a = diffuseColor.a * 2.0 * tex1.a;
    }
    else if (pixelId == 69) //CModelPixelShaderID::Mod2x_Mod)
    {
        result.rgb = diffuseColor.rgb * 2.0 * tex1.rgb * tex2.rgb;
        result.a = diffuseColor.a * 2.0 * tex1.a * tex2.a;
    }
    else if (pixelId == 70) //CModelPixelShaderID::Mod2x_Mod2x)
    {
        result = diffuseColor * 4.0 * tex1 * tex2;
    }
    else if (pixelId == 71) //CModelPixelShaderID::Add)
    {
        result = diffuseColor + tex1;
    }
    else if (pixelId == 72) //CModelPixelShaderID::Add_Mod)
    {
        result.rgb = (diffuseColor.rgb + tex1.rgb) * tex2.a;
        result.a = (diffuseColor.a + tex1.a) * tex2.a;
    }
    else if (pixelId == 73) //CModelPixelShaderID::Fade)
    {
        result.rgb = (tex1.rgb - diffuseColor.rgb) * diffuseColor.a + diffuseColor.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 74) //CModelPixelShaderID::Decal)
    {
        result.rgb = (diffuseColor.rgb - tex1.rgb) * diffuseColor.a + tex1.rgb;
        result.a = diffuseColor.a;
    }

    return result;
}

#ifdef DEPTH_PASS

void main()
{
    // depth/shadow pass: run the same combiner the forward pass uses so the cutout alpha matches
    // exactly, then discard; no color/lighting/decals/output.
    vec4 tex1 = SAMPLE_BINDLESS(M(texture1Index), TexCoord.xy);
    vec4 tex2 = SAMPLE_BINDLESS(M(texture2Index), TexCoord2.xy);
    vec4 tex3 = SAMPLE_BINDLESS(M(texture3Index), TexCoord2.xy);

    vec4 col = ShadeCModel(M(pixel_shader), tex1, tex2, tex3) * VertexColor;
    if (col.a < M(alphaTest))
        discard;

    float makeTranslucent = mix(float(1), float(int((gl_FragCoord.x + gl_FragCoord.y)) % 2), float(M(translucent)));
    if (makeTranslucent < 0.5)
        discard;
}

#else

void main()
{
    PIXEL_SETUP_INSTANCING(instanceID);

    vec4 tex1 = SAMPLE_BINDLESS(M(texture1Index), TexCoord.xy);
    vec4 tex2 = SAMPLE_BINDLESS(M(texture2Index), TexCoord2.xy);
    vec4 tex3 = SAMPLE_BINDLESS(M(texture3Index), TexCoord2.xy);

    vec4 col = ShadeCModel(M(pixel_shader), tex1, tex2, tex3) * VertexColor;

    if (col.a < M(alphaTest))
        discard;

    float makeTranslucent = mix(float(1), float(int((gl_FragCoord.x + gl_FragCoord.y)) % 2), float(M(translucent)));
    if (makeTranslucent < 0.5)
        discard;

    uint decalPickId;
    col.rgb = ApplyDecals(col.rgb, Normal.xyz, WorldPos.xyz, decalPickId);

    vec3 lighted = lighting(col.rgb, Normal.xyz, WorldPos.xyz);
    FragColor = vec4(mix(lighted, col.rgb, float(M(unlit))), col.a);

    vec4 highlighted = FragColor + vec4(0.1, 0.1, 0.1, 0.0);
    FragColor = mix(FragColor, highlighted, M(highlight));

    FragColor = vec4(mix(FragColor.rgb, vec3(1.0, 0.0, 0.0), M(notSupported)), col.a);
    ObjectIndexOutputBuffer = decalPickId != 0u ? decalPickId : objectIndex;
    FragColor = ApplyFog(FragColor, WorldPos.xyz);
}

#endif
