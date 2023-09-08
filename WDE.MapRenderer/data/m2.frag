#version 330 core
#include "../internalShaders/theengine.cginc"
in vec4 Color;
in vec2 TexCoord;
in vec2 TexCoord2;
in vec4 WorldPos;
in vec4 SplatId;
in vec3 Normal;
flat in int instanceID;
layout (location = 0) out vec4 FragColor;
layout (location = 1) out uint ObjectIndexOutputBuffer;

uniform sampler2D texture1;
uniform sampler2D texture2;
uniform sampler2D texture3;
uniform float alphaTest;
uniform float notSupported;
uniform float highlight;
uniform int unlit;
uniform int pixel_shader;
uniform int translucent;
uniform vec4 mesh_color;

vec4 ShadeCModel(int pixelId, vec4 texture1, vec4 texture2, vec4 texture3)
{
    vec4 result = vec4(0, 0, 0, 1) + mesh_color * 0.00001;
    vec4 diffuseColor = vec4(1, 1, 1, 1);
    vec4 pixelColor1 = vec4(1, 1, 1, 1);
    vec3 specular = vec3(0, 0, 0);
    bool canDiscard = false;

    //if (pixelId == 9)
    //    return vec4(1,0,0,1);

    // based on Deamon's code, thank you!
    // https://github.com/Deamon87/WebWowViewerCpp/blob/master/wowViewerLib/shaders/glsl/vulkan/m2Shader.frag
    if (pixelId == 0) { //Combiners_Opaque
        result.rgb = diffuseColor.rgb * texture1.rgb;
    } else if (pixelId == 1) { //Combiners_Mod
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a;
        canDiscard = true;
    } else if (pixelId == 2) { //Combiners_Opaque_Mod
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = texture2.a;
        canDiscard = true;
    } else if (pixelId == 3) { //Combiners_Opaque_Mod2x
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb * 2.0;
        result.a = texture2.a * 2.0;
        canDiscard = true;
    } else if (pixelId == 4) { //Combiners_Opaque_Mod2xNA
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb * 2.0;
    } else if (pixelId == 5) { //Combiners_Opaque_Opaque
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
    }
    else if (pixelId == 6) { //Combiners_Mod_Mod
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = texture1.a * texture2.a;
        canDiscard = true;
    } else if (pixelId == 7) { //Combiners_Mod_Mod2x
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb * 2.0;
        result.a = texture1.a * texture2.a * 2.0;
        canDiscard = true;
    } else if (pixelId == 8) { //Combiners_Mod_Add
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a + texture2.a;
        canDiscard = true;
        //specular = texture2.rgb;
    } else if (pixelId == 9) { //Combiners_Mod_Mod2xNA
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb * 2.0;
        result.a = texture1.a;
        canDiscard = true;
    } else if (pixelId == 10) { //Combiners_Mod_AddNA
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a;
        canDiscard = true;
        //specular = texture2.rgb;
    } else if (pixelId == 11) { //Combiners_Mod_Opaque
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = texture1.a;
        canDiscard = true;
    } else if (pixelId == 12) { //Combiners_Opaque_Mod2xNA_Alpha
        result.rgb = diffuseColor.rgb * mix(texture1.rgb * texture2.rgb * 2.0, texture1.rgb, vec3(texture1.a));
    } else if (pixelId == 13) { //Combiners_Opaque_AddAlpha
        result.rgb = diffuseColor.rgb * texture1.rgb;
        //specular = texture2.rgb * texture2.a;
    } else if (pixelId == 14) { //Combiners_Opaque_AddAlpha_Alpha
        result.rgb = diffuseColor.rgb * texture1.rgb;
        //specular = texture2.rgb * texture2.a * (1.0 - texture1.a);
    } else if (pixelId == 15) { //Combiners_Opaque_Mod2xNA_Alpha_Add
        result.rgb = diffuseColor.rgb * mix(texture1.rgb * texture2.rgb * 2.0, texture1.rgb, vec3(texture1.a));
        //specular = tex3.rgb * tex3.a * uTexSampleAlpha.b;
    } else if (pixelId == 16) { //Combiners_Mod_AddAlpha
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a;
        canDiscard = true;
        //specular = texture2.rgb * texture2.a;
    } else if (pixelId == 17) { //Combiners_Mod_AddAlpha_Alpha
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a + texture2.a * (0.3 * texture2.r + 0.59 * texture2.g + 0.11 * texture2.b);
        canDiscard = true;
        //specular = texture2.rgb * texture2.a * (1.0 - texture1.a);
    } else if (pixelId == 18) { //Combiners_Opaque_Alpha_Alpha
        result.rgb = diffuseColor.rgb * mix(mix(texture1.rgb, texture2.rgb, vec3(texture2.a)), texture1.rgb, vec3(texture1.a));
    } else if (pixelId == 19) { //Combiners_Opaque_Mod2xNA_Alpha_3s
        result.rgb = diffuseColor.rgb * mix(texture1.rgb * texture2.rgb * 2.0, texture3.rgb, vec3(texture3.a));
    } else if (pixelId == 20) { //Combiners_Opaque_AddAlpha_Wgt
        result.rgb = diffuseColor.rgb * texture1.rgb;
        //specular = texture2.rgb * texture2.a * uTexSampleAlpha.g;
    } else if (pixelId == 21) { //Combiners_Mod_Add_Alpha
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a + texture2.a;
        canDiscard = true;
        //specular = texture2.rgb * (1.0 - texture1.a);
    } else if (pixelId == 22) { //Combiners_Opaque_ModNA_Alpha
        result.rgb = diffuseColor.rgb * mix(texture1.rgb * texture2.rgb, texture1.rgb, vec3(texture1.a));
    }
    else if (pixelId == 50)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 51) // CModelPixelShaderID::Opaque_Opaque
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 52) // CModelPixelShaderID::Opaque_Mod
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = texture2.a * diffuseColor.a;
    }
    else if (pixelId == 53) // CModelPixelShaderID::Opaque_Mod2x
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * 2.0f * texture2.a;
    }
    else if (pixelId == 54) // CModelPixelShaderID::Opaque_Mod2xNA
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 55) // CModelPixelShaderID::Opaque_Add)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb + texture2.rgb;
        result.a = diffuseColor.a + texture1.a;
    }
    else if (pixelId == 56) // CModelPixelShaderID::Opaque_AddNA)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb + texture2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 57) //CModelPixelShaderID::Opaque_AddAlpha)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a;

        specular = texture2.rgb * texture2.a;
    }
    else if (pixelId == 58)//CModelPixelShaderID::Opaque_AddAlpha_Alpha)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a;

        specular = texture2.rgb * texture2.a * (1.0f - texture1.a);
    }
    else if (pixelId == 59)//CModelPixelShaderID::Opaque_Mod2xNA_Alpha)
    {
        result.rgb = diffuseColor.rgb * mix(texture1.rgb * texture2.rgb * 2.0f, texture1.rgb, texture1.aaa);
        result.a = diffuseColor.a;
    }
    else if (pixelId == 60)//CModelPixelShaderID::Mod)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a * texture1.a;
    }
    else if (pixelId == 61)//CModelPixelShaderID::Mod_Opaque)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * texture1.a;
    }
    else if (pixelId == 62)//CModelPixelShaderID::Mod_Mod)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * texture1.a * texture2.a;
    }
    else if (pixelId == 63)//CModelPixelShaderID::Mod_Mod2x)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * 2.0f * texture1.a * texture2.a;
    }
    else if (pixelId == 64)//CModelPixelShaderID::Mod_Mod2xNA)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = texture1.a * diffuseColor.a;
    }
    else if (pixelId == 65)//CModelPixelShaderID::Mod_Add)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a * (texture1.a + texture2.a);

        specular = texture2.rgb;
    }
    else if (pixelId == 67)//CModelPixelShaderID::Mod_AddNA)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a * diffuseColor.a;

        specular = texture2.rgb;
    }
    else if (pixelId == 68)//CModelPixelShaderID::Mod2x)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb;
        result.a = diffuseColor.a * 2.0f * texture1.a;
    }
    else if (pixelId == 69)//CModelPixelShaderID::Mod2x_Mod)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * 2.0f * texture1.a * texture2.a;
    }
    else if (pixelId == 70)//CModelPixelShaderID::Mod2x_Mod2x)
    {
        result = diffuseColor * 4.0f * texture1 * texture2;
    }
    else if (pixelId == 71)//CModelPixelShaderID::Add)
    {
        result = diffuseColor + texture1;
    }
    else if (pixelId == 72)//CModelPixelShaderID::Add_Mod)
    {
        result.rgb = (diffuseColor.rgb + texture1.rgb) * texture2.a;
        result.a = (diffuseColor.a + texture1.a) * texture2.a;
    }
    else if (pixelId == 73)//CModelPixelShaderID::Fade)
    {
        result.rgb = (texture1.rgb - diffuseColor.rgb) * diffuseColor.a + diffuseColor.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 74)//CModelPixelShaderID::Decal)
    {
        result.rgb = (diffuseColor.rgb - texture1.rgb) * diffuseColor.a + texture1.rgb;
        result.a = diffuseColor.a;
    }

    return result;
}

void main()
{
    PIXEL_SETUP_INSTANCING(instanceID);

    vec4 tex1 = texture(texture1, TexCoord.xy);
    vec4 tex2 = texture(texture2, TexCoord2.xy);
    vec4 tex3 = texture(texture3, TexCoord2.xy);

    vec4 color = ShadeCModel(pixel_shader, tex1, tex2, tex3);

    if (color.a < alphaTest)
        discard;

    float makeTranslucent = mix(float(1), float(int((gl_FragCoord.x + gl_FragCoord.y)) % 2), float(translucent));
    if (makeTranslucent < 0.5)
        discard;

    vec3 lighted = lighting(color.rgb, Normal.xyz);
    FragColor = vec4(mix(lighted, color.rgb, unlit), color.a);

    vec4 highglighted = FragColor + vec4(0.1, 0.1, 0.1, 0);
    FragColor = mix(FragColor, highglighted, highlight);

    FragColor = vec4(mix(FragColor.rgb, vec3(1, 0, 0), notSupported), color.a);
    ObjectIndexOutputBuffer = objectIndex;
    FragColor = ApplyFog(FragColor, WorldPos.xyz);
}