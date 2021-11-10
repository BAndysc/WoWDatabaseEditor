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
uniform int shader_id;
/*uniform bool use_vertex_color;
uniform bool unlit;
uniform bool exterior_lit;*/

vec3 lighting(vec3 material)
{
    float diff = max(dot(Normal, -lightDir.xyz), 0.0);
    vec3 diffuse = diff * lightColor.rgb;
    vec3 ambient = vec3(1, 1, 1) * 0.4;
    return material * (diffuse + ambient);

    /*vec3 light_color = vec3(1.);
    vec3 vertex_color = use_vertex_color ? Color.rgb : vec3(0.);
    
    if (unlit)
    {
        light_color = vertex_color + (exterior_lit ? exterior_ambient_color : ambient_color);
    }
    else if(exterior_lit)
    {
        vec3 ambient = exterior_ambient_color + vertex_color.rgb;
    
        light_color = vec3(clamp (exterior_diffuse_color * max(dot(f_normal, exterior_light_dir), 0.0), 0.0, 1.0)) + ambient;
    }
    else
    {
        light_color = ambient_color + vertex_color.rgb;
    }  
    
    return material * light_color;*/
}

void main()
{   
    vec4 tex = texture(texture1, TexCoord.xy);
    vec4 tex_2 = texture(texture2, TexCoord.xy);

    if (tex.a < alphaTest) 
        discard;

    //FragColor = vec4(tex.rgb * (diffuse + ambient), mix(1, tex.a, (sign(alphaTest) + 1) / 2));
    
    // see: https://github.com/Deamon87/WebWowViewerCpp/blob/master/wowViewerLib/src/glsl/wmoShader.glsl
    if(shader_id == 3) // Env
    {
        vec3 env = tex_2.rgb * tex.rgb;
        FragColor = vec4(lighting(tex.rgb) + env, 1.);
    }
    else if(shader_id == 5) // EnvMetal
    {
        vec3 env = tex_2.rgb * tex.rgb * tex.a;
        FragColor = vec4(lighting(tex.rgb) + env, 1.);
    }
    else if(shader_id == 6) // TwoLayerDiffuse
    {
        vec3 layer2 = mix(tex.rgb, tex_2.rgb, tex_2.a);
        FragColor = vec4(lighting(mix(layer2, tex.rgb, Color.a)), 1.);
    }
    else // default shader, used for shader_id 0,1,2,4 (Diffuse, Specular, Metal, Opaque)
    {
        FragColor = vec4(lighting(tex.rgb), 1.);
    }    
    
    //FragColor = vec4(mix(FragColor.rgb, vec3(1, 0, 0), notSupported), FragColor.a);
}