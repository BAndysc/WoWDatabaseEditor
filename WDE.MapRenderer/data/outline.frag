#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D outlineTex;
uniform sampler2D outlineTexUnBlurred;
uniform sampler2D _MainTex;
uniform vec4 outlineColor;

void main()
{ 
    vec4 tex = texture(_MainTex, vec2(TexCoords.x, TexCoords.y));
    vec4 outline = textureLod(outlineTex, vec2(TexCoords.x, TexCoords.y), 0);

    for (int x = -1; x <= 1; ++x)
    {
        for (int y = -1; y <= 1; ++y)
        {
            outline += textureLod(outlineTex, vec2(TexCoords.x, TexCoords.y) + vec2(x, y) * 0.0025, 0);
        }
    }
    outline = outline / (1+3 * 3);

    vec4 outlineNoBlur = textureLod(outlineTexUnBlurred, vec2(TexCoords.x, TexCoords.y), 0);
    
    if (outlineNoBlur.w == 1)
    {
        FragColor = tex;
    }
    else
    {
        float line = smoothstep(0, 0.5, outline.w);
        FragColor = vec4(outlineColor.xyz, 1) * line + tex * (1-line);
    }
    //FragColor = outline + outlineNoBlur * 0.0001;
}