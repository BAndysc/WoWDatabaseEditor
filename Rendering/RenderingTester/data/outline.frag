#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D outlineTex;
uniform sampler2D _MainTex;

void main()
{ 
    vec4 tex = texture(_MainTex, vec2(TexCoords.x, TexCoords.y));
    vec4 outline = texture(outlineTex, vec2(TexCoords.x, TexCoords.y));
    
    if (outline.w == 1)
    {
        FragColor = vec4(0);
    }
    else
    {
        FragColor = outline;
    }
    
}