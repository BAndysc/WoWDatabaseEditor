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

uniform int pixel_shader;
uniform vec4 mesh_color;

void main()
{
    vec4 tex1 = texture(texture1, TexCoord.xy);
    vec4 tex2 = texture(texture2, TexCoord2.xy);

    vec4 color = vec4(0, 0, 0, 0);
    
    // code from Deamon87 and https://wowdev.wiki/M2/Rendering#Pixel_Shaders
    if (pixel_shader == 0) //Combiners_Opaque
    { 
        color.rgb = tex1.rgb * mesh_color.rgb;
        color.a = mesh_color.a;
        if (tex1.a < alphaTest)
            discard;
    } 
    else if (pixel_shader == 1) // Combiners_Decal
    { 
        color.rgb = mix(mesh_color.rgb, tex1.rgb, mesh_color.a);
        color.a = mesh_color.a;
    } 
    else if (pixel_shader == 2) // Combiners_Add
    { 
        color.rgba = tex1.rgba + mesh_color.rgba;
    } 
    else if (pixel_shader == 3) // Combiners_Mod2x
    { 
        color.rgb = tex1.rgb * mesh_color.rgb * vec3(2.0);
        color.a = tex1.a * mesh_color.a * 2.0;
    } 
    else if (pixel_shader == 4) // Combiners_Fade
    { 
        color.rgb = mix(tex1.rgb, mesh_color.rgb, mesh_color.a);
        color.a = mesh_color.a;
    } 
    else if (pixel_shader == 5) // Combiners_Mod
    { 
        color.rgba = tex1.rgba * mesh_color.rgba;
    } 
    else if (pixel_shader == 6) // Combiners_Opaque_Opaque
    { 
        color.rgb = tex1.rgb * tex2.rgb * mesh_color.rgb;
        color.a = mesh_color.a;
    } 
    else if (pixel_shader == 7) // Combiners_Opaque_Add
    { 
        color.rgb = tex2.rgb + tex1.rgb * mesh_color.rgb;
        color.a = mesh_color.a + tex1.a;
    } 
    else if (pixel_shader == 8) // Combiners_Opaque_Mod2x
    { 
        color.rgb = tex1.rgb * mesh_color.rgb * tex2.rgb * vec3(2.0);
        color.a  = tex2.a * mesh_color.a * 2.0;
    } 
    else if (pixel_shader == 9)  // Combiners_Opaque_Mod2xNA
    {
        color.rgb = tex1.rgb * mesh_color.rgb * tex2.rgb * vec3(2.0);
        color.a  = mesh_color.a;
    } 
    else if (pixel_shader == 10) // Combiners_Opaque_AddNA
    { 
        color.rgb = tex2.rgb + tex1.rgb * mesh_color.rgb;
        color.a = mesh_color.a;
    } 
    else if (pixel_shader == 11) // Combiners_Opaque_Mod
    { 
        color.rgb = tex1.rgb * tex2.rgb * mesh_color.rgb;
        color.a = tex2.a * mesh_color.a;
    } 
    else if (pixel_shader == 12) // Combiners_Mod_Opaque
    { 
        color.rgb = tex1.rgb * tex2.rgb * mesh_color.rgb;
        color.a = tex1.a;
    } 
    else if (pixel_shader == 13) // Combiners_Mod_Add
    { 
        color.rgba = tex2.rgba + tex1.rgba * mesh_color.rgba;
    } 
    else if (pixel_shader == 14) // Combiners_Mod_Mod2x
    { 
        color.rgba = tex1.rgba * tex2.rgba * mesh_color.rgba * vec4(2.0);
    } 
    else if (pixel_shader == 15) // Combiners_Mod_Mod2xNA
    { 
        color.rgb = tex1.rgb * tex2.rgb * mesh_color.rgb * vec3(2.0);
        color.a = tex1.a * mesh_color.a;
    } 
    else if (pixel_shader == 16) // Combiners_Mod_AddNA
    { 
        color.rgb = tex2.rgb + tex1.rgb * mesh_color.rgb;
        color.a = tex1.a * mesh_color.a;
    } 
    else if (pixel_shader == 17) // Combiners_Mod_Mod
    { 
        color.rgba = tex1.rgba * tex2.rgba * mesh_color.rgba;
    } 
    else if (pixel_shader == 18) // Combiners_Add_Mod
    { 
        color.rgb = (tex1.rgb + mesh_color.rgb) * tex2.a;
        color.a = (tex1.a + mesh_color.a) * tex2.a;
    } 
    else if (pixel_shader == 19) // Combiners_Mod2x_Mod2x
    {
        color.rgba = tex1.rgba * tex2.rgba * mesh_color.rgba * vec4(4.0);
    }
    else if (pixel_shader == 20)  // Combiners_Opaque_Mod2xNA_Alpha
    {
        color.rgb = (mesh_color.rgb * tex1.rgb) * mix(tex2.rgb * 2.0, vec3(1.0), tex1.a);
        color.a = mesh_color.a;
    }
    else if (pixel_shader == 21)   //Combiners_Opaque_AddAlpha
    {
        color.rgb = (mesh_color.rgb * tex1.rgb) + (tex2.rgb * tex2.a);
        color.a = mesh_color.a;
    }
    else if (pixel_shader == 22)   // Combiners_Opaque_AddAlpha_Alpha
    {
        color.rgb = (mesh_color.rgb * tex1.rgb) + (tex2.rgb * tex2.a * tex1.a);
        color.a = mesh_color.a;
    }

    if (color.a < alphaTest) 
        discard;
        
    FragColor = vec4(lighting(color.rgb, Normal.xyz), color.a);
    
    vec4 highglighted = FragColor + vec4(0.1, 0.1, 0.1, 0);
    FragColor = mix(FragColor, highglighted, highlight);
    
    FragColor = vec4(mix(FragColor.rgb, vec3(1, 0, 0), notSupported), FragColor.a);
}