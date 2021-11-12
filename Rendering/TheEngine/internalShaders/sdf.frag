#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform vec4 fillColor;
uniform int mode;
uniform sampler2D font;

void main()
{ 
    float u_buffer = 0.5;
    float u_gamma = 0.1;
    float t = texture(font, vec2(TexCoords.x, TexCoords.y)).a;

    float alpha = smoothstep(u_buffer - u_gamma, u_buffer + u_gamma, t);
    alpha = mix(alpha, 1, mode); // 0 - alpha, 1 - const one
    FragColor = vec4(1, 1, 1, alpha) * fillColor;
    
}