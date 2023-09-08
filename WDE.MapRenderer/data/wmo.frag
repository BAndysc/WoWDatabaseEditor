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
uniform float alphaTest;
uniform float notSupported;
uniform int shader_id;
uniform int unlit;
uniform int brightAtNight;
uniform int interior;
uniform int translucent;

/*uniform bool use_vertex_color;
uniform bool unlit;
uniform bool exterior_lit;*/

int or(int a, int b)
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

void main()
{
    PIXEL_SETUP_INSTANCING(instanceID);
    
    vec4 tex = texture(texture1, TexCoord.xy);
    vec4 tex_2 = texture(texture2, TexCoord.xy);

    if (tex.a < alphaTest) 
        discard;

    float makeTranslucent = mix(float(1), float(int((gl_FragCoord.x + gl_FragCoord.y)) % 2), float(translucent));
    if (makeTranslucent < 0.5)
        discard;
    
    //FragColor = vec4(tex.rgb * (diffuse + ambient), mix(1, tex.a, (sign(alphaTest) + 1) / 2));
    
    vec3 diffuse;
    
    // see: https://github.com/Deamon87/WebWowViewerCpp/blob/master/wowViewerLib/src/glsl/wmoShader.glsl
    if(shader_id == 3) // Env
    {
        vec3 env = tex_2.rgb * tex.rgb;
        diffuse = tex.rgb + env;
    }
    else if(shader_id == 5) // EnvMetal
    {
        vec3 env = tex_2.rgb * tex.rgb * tex.a;
        diffuse = tex.rgb + env;
    }
    else if(shader_id == 6) // TwoLayerDiffuse
    {
        vec3 layer2 = mix(tex.rgb, tex_2.rgb, tex_2.a);
        diffuse = mix(layer2, tex.rgb, Color.a);
    }
    else // default shader, used for shader_id 0,1,2,4 (Diffuse, Specular, Metal, Opaque)
    {
        diffuse = tex.xyz;
    }
    
    vec3 lighted = lighting(diffuse, Normal.xyz);
    vec3 interiorLight = diffuse * max3(Color.rgb, vec3(0.3));
    vec3 finalColor = mix(lighted, interiorLight, interior);
    finalColor = mix(finalColor, diffuse, or(brightAtNight, unlit));

    FragColor = ApplyFog(vec4(finalColor, 1), WorldPos.xyz);
    ObjectIndexOutputBuffer = objectIndex;
  
    //FragColor = vec4(mix(FragColor.rgb, vec3(1, 0, 0), notSupported), FragColor.a);
}