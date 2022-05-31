#version 330 core
out vec4 FragColor;
  
in vec2 TexCoords;

uniform sampler2D _MainTex;
uniform int horizontalPass; // 0 or 1 to indicate vertical or horizontal pass
uniform float sigma;        // The sigma value for the gaussian function: higher value means more blur
                            // A good value for 9x9 is around 3 to 5
                            // A good value for 7x7 is around 2.5 to 4
                            // A good value for 5x5 is around 2 to 3.5

#define PI 3.14159265359
#define E 2.71828182846

const vec2 texOffset = vec2(1.0, 1.0);

#define SAMPLES 20
uniform float blurSize;
uniform vec4 direction;
#define _StandardDeviation 0.02

void main()
{    
    vec4 col = vec4(0);
    float sum = 0;
    for (float index = 0; index < SAMPLES; index++){
        //get the offset of the sample
        float offset = (index/(SAMPLES-1) - 0.5) * blurSize;
        //get uv coordinate of sample
        vec2 uv = TexCoords.xy + offset * direction.xy;
        //calculate the result of the gaussian function
        float stDevSquared = _StandardDeviation*_StandardDeviation;
        float gauss = (1 / sqrt(2*PI*stDevSquared)) * pow(E, -((offset*offset)/(2*stDevSquared)));
        //add result to sum
        sum += gauss;
        //multiply color with influence from gaussian function and add it to sum color
        col += texture(_MainTex, uv) * gauss;
    }
    //divide the sum of values by the amount of samples
    col = col / sum;
    FragColor = col;
}