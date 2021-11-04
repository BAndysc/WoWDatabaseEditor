#include "theengine.cginc"

struct VertexInputType
{
	float4 position : POSITION;
    float4 color : COLOR0;
    float4 normal : NORMAL;
	float2 uv1 : TEXCOORD0;
	float2 uv2 : TEXCOORD1;
};

struct PixelInputType
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

PixelInputType vert(VertexInputType input)
{
	PixelInputType output;
	
	output.position = float4(input.position.x, input.position.y, 0, 1);

    output.uv = float2(input.position.x / 2 + 0.5, input.position.y / -2 + 0.5);
	
	return output;
}