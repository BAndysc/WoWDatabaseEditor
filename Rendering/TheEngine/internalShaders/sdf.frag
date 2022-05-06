#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform vec4 fillColor;
uniform int mode;
uniform sampler2D font;

void main()
{ 
    float u_buffer = 0.25;
    float u_gamma = 0.03;
    float t = texture(font, vec2(TexCoords.x, TexCoords.y)).a;

    float alpha = smoothstep(u_buffer - u_gamma, u_buffer + u_gamma, t);
    alpha = mix(alpha, 1, mode); // 0 - alpha, 1 - const one
    FragColor = vec4(0, 0, 0, alpha);

    u_buffer = 0.45;
    float outline = smoothstep(u_buffer - u_gamma, u_buffer + u_gamma, t);
    FragColor = mix(FragColor, vec4(1, 1, 1, 1) * fillColor, outline);    
}