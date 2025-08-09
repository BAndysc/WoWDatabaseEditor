#version 330 core
#include "../internalShaders/theengine.cginc"
in vec2 Barys;
out vec4 FragColor;

uniform vec4 color;
uniform float width;

void main()
{
    vec3 barys = vec3(Barys.xy, 1 - Barys.x - Barys.y);
	vec3 deltas = fwidth(barys);
	barys = smoothstep(vec3(0), deltas * width, barys);
	float minBary = min(barys.x, min(barys.y, barys.z));
	if (minBary > 0.5) 
	    discard;
    FragColor = color;
}