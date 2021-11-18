#include "theengine.cginc"

struct VertexInputType
{
	float4 position : POSITION;
    float4 color : COLOR0;
    float4 normal : NORMAL;
	float2 uv1 : TEXCOORD0;
    VERTEX_INSTANCING
};

struct PixelInputType
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
};

PixelInputType vert(VertexInputType input)
{
	VERTEX_SETUP_INSTANCING

	PixelInputType output;
	
	output.position = mul(float4(input.position.xyz, 1), worldMatrix);
	output.position = mul(output.position, viewMatrix);
	output.position = mul(output.position, projectionMatrix);
	
	output.uv = input.uv1;

	return output;
}