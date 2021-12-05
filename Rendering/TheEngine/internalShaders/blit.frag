#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D texture1;
uniform int flipY;

void main()
{ 
    FragColor = texture(texture1, vec2(TexCoords.x, mix(TexCoords.y, 1-TexCoords.y, flipY)));
}