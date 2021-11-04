#if VERTEX_SHADER
cbuffer SceneBuffer : register(b12)
{
    row_major matrix viewMatrix;
    row_major matrix projectionMatrix;
	float4 cameraPosition;
	float3 lightPosition;
	
		float align0;

    float time;

    float align1;
    float align2;
    float align3;
};

cbuffer ObjectBuffer : register(b13)
{
    row_major matrix worldMatrix;
};


#if INSTANCING
#define VERTEX_INSTANCING float4 InstancePos0 : TEXCOORD2; \
	float4 InstancePos1 : TEXCOORD3; \
	float4 InstancePos2 : TEXCOORD4; \
	float4 InstancePos3 : TEXCOORD5;

#define VERTEX_SETUP_INSTANCING float4x4 worldMatrix = float4x4(input.InstancePos0, input.InstancePos1, input.InstancePos2, input.InstancePos3);
#else
#define VERTEX_INSTANCING ;
#define VERTEX_SETUP_INSTANCING ;
#endif


#endif

#if PIXEL_SHADER
cbuffer SceneBuffer : register(b13)
{
    float4 lightDirection;
    float4 lightColor;
	float3 lightPosition;

	float align0;

    float time;

    float screenWidth;
    float screenHeight;
    float align3;
};

SamplerState TheDefaultSampler : register(s15);

#define tex3D(TEX, UV, INDEX) (TEX.Sample(TheDefaultSampler, float3(UV, INDEX)))
#define tex2D(TEX, UV) (TEX.Sample(TheDefaultSampler, UV))
#endif