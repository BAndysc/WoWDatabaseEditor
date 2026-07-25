#version 460

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inColor;

layout(location = 0) out vec3 fragColor;

layout(push_constant) uniform Push
{
    float angle;
    float aspect;
    vec2 pad;
} push;

void main()
{
    float c = cos(push.angle);
    float s = sin(push.angle);
    vec2 p = vec2(inPosition.x * c - inPosition.y * s,
                  inPosition.x * s + inPosition.y * c);
    // GL-style clip space (+y up); the app uses a negative-height viewport
    gl_Position = vec4(p.x / push.aspect, p.y, inPosition.z, 1.0);
    fragColor = inColor;
}
