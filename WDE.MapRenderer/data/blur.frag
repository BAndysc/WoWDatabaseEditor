#version 450
#extension GL_EXT_nonuniform_qualifier : require
layout(set = 2, binding = 0) uniform texture2D bindlessTextures[16384];
layout(set = 2, binding = 1) uniform sampler bindlessSamplers[128];
#define SAMPLE_BINDLESS(idx, uv) texture(sampler2D(bindlessTextures[nonuniformEXT(uint(idx) & 0xFFFFFu)], bindlessSamplers[nonuniformEXT(uint(idx) >> 20u)]), uv)

layout(location = 0) out vec4 FragColor;

layout(location = 0) in vec2 TexCoords;

layout(std140, set = 1, binding = 0) uniform MaterialData
{
    int horizontalPass;
    float sigma;
    float blurSize;
    int mainTexIndex;
    vec4 direction;
};

#define PI 3.14159265359
#define E 2.71828182846

const vec2 texOffset = vec2(1.0, 1.0);

#define SAMPLES 20
#define _StandardDeviation 0.02

void main()
{
    vec4 col = vec4(0);
    float sum = 0.0;
    for (float index = 0.0; index < SAMPLES; index++){
        float offset = (index/(SAMPLES-1) - 0.5) * blurSize;
        vec2 uv = TexCoords.xy + offset * direction.xy;
        float stDevSquared = _StandardDeviation*_StandardDeviation;
        float gauss = (1.0 / sqrt(2.0*PI*stDevSquared)) * pow(E, -((offset*offset)/(2.0*stDevSquared)));
        sum += gauss;
        col += SAMPLE_BINDLESS(mainTexIndex, uv) * gauss;
    }
    col = col / sum;
    FragColor = col;
}
