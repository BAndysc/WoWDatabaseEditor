#version 330 core
#include "../internalShaders/theengine.cginc"
in vec4 Color;
in vec2 TexCoord;
in vec2 TexCoord2;
in vec4 WorldPos;
in vec4 SplatId;
in vec3 Normal;
out vec4 FragColor;

uniform sampler2D texture1;
uniform sampler2D texture2;
uniform float alphaTest;
uniform float notSupported;
uniform float highlight;
uniform int unlit;
uniform int pixel_shader;
uniform vec4 mesh_color;

vec4 ShadeCModel(int pixelId, vec4 texture1, vec4 texture2)
{
    vec4 result = vec4(0, 0, 0, 0) + mesh_color * 0.00001;
    vec4 diffuseColor = vec4(1, 1, 1, 1);
    vec3 specular = vec3(0, 0, 0);

    if (pixelId == 0)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 1) // CModelPixelShaderID::Opaque_Opaque
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 2) // CModelPixelShaderID::Opaque_Mod
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = texture2.a * diffuseColor.a;
    }
    else if (pixelId == 3) // CModelPixelShaderID::Opaque_Mod2x
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * 2.0f * texture2.a;
    }
    else if (pixelId == 4) // CModelPixelShaderID::Opaque_Mod2xNA
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 5) // CModelPixelShaderID::Opaque_Add)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb + texture2.rgb;
        result.a = diffuseColor.a + texture1.a;
    }
    else if (pixelId == 6) // CModelPixelShaderID::Opaque_AddNA)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb + texture2.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 7) //CModelPixelShaderID::Opaque_AddAlpha)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a;

        specular = texture2.rgb * texture2.a;
    }
    else if (pixelId == 8)//CModelPixelShaderID::Opaque_AddAlpha_Alpha)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a;

        specular = texture2.rgb * texture2.a * (1.0f - texture1.a);
    }
    else if (pixelId == 9)//CModelPixelShaderID::Opaque_Mod2xNA_Alpha)
    {
        result.rgb = diffuseColor.rgb * mix(texture1.rgb * texture2.rgb * 2.0f, texture1.rgb, texture1.aaa);
        result.a = diffuseColor.a;
    }
    else if (pixelId == 10)//CModelPixelShaderID::Mod)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a * texture1.a;
    }
    else if (pixelId == 11)//CModelPixelShaderID::Mod_Opaque)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * texture1.a;
    }
    else if (pixelId == 12)//CModelPixelShaderID::Mod_Mod)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * texture1.a * texture2.a;
    }
    else if (pixelId == 13)//CModelPixelShaderID::Mod_Mod2x)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * 2.0f * texture1.a * texture2.a;
    }
    else if (pixelId == 14)//CModelPixelShaderID::Mod_Mod2xNA)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = texture1.a * diffuseColor.a;
    }
    else if (pixelId == 15)//CModelPixelShaderID::Mod_Add)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = diffuseColor.a * (texture1.a + texture2.a);

        specular = texture2.rgb;
    }
    else if (pixelId == 17)//CModelPixelShaderID::Mod_AddNA)
    {
        result.rgb = diffuseColor.rgb * texture1.rgb;
        result.a = texture1.a * diffuseColor.a;

        specular = texture2.rgb;
    }
    else if (pixelId == 18)//CModelPixelShaderID::Mod2x)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb;
        result.a = diffuseColor.a * 2.0f * texture1.a;
    }
    else if (pixelId == 19)//CModelPixelShaderID::Mod2x_Mod)
    {
        result.rgb = diffuseColor.rgb * 2.0f * texture1.rgb * texture2.rgb;
        result.a = diffuseColor.a * 2.0f * texture1.a * texture2.a;
    }
    else if (pixelId == 20)//CModelPixelShaderID::Mod2x_Mod2x)
    {
        result = diffuseColor * 4.0f * texture1 * texture2;
    }
    else if (pixelId == 21)//CModelPixelShaderID::Add)
    {
        result = diffuseColor + texture1;
    }
    else if (pixelId == 22)//CModelPixelShaderID::Add_Mod)
    {
        result.rgb = (diffuseColor.rgb + texture1.rgb) * texture2.a;
        result.a = (diffuseColor.a + texture1.a) * texture2.a;
    }
    else if (pixelId == 23)//CModelPixelShaderID::Fade)
    {
        result.rgb = (texture1.rgb - diffuseColor.rgb) * diffuseColor.a + diffuseColor.rgb;
        result.a = diffuseColor.a;
    }
    else if (pixelId == 24)//CModelPixelShaderID::Decal)
    {
        result.rgb = (diffuseColor.rgb - texture1.rgb) * diffuseColor.a + texture1.rgb;
        result.a = diffuseColor.a;
    }

    return result;
}

void main()
{
    vec4 tex1 = texture(texture1, TexCoord.xy);
    vec4 tex2 = texture(texture2, TexCoord2.xy);

    vec4 color = ShadeCModel(pixel_shader, tex1, tex2);

    if (color.a < alphaTest) 
        discard;
    
    vec3 lighted = lighting(color.rgb, Normal.xyz);
    FragColor = vec4(mix(lighted, color.rgb, unlit), color.a);
    
    vec4 highglighted = FragColor + vec4(0.1, 0.1, 0.1, 0);
    FragColor = mix(FragColor, highglighted, highlight);
    
    FragColor = vec4(mix(FragColor.rgb, vec3(1, 0, 0), notSupported), color.a);
}