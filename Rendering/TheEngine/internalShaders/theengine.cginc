#define PI 3.14159265358979323846
#define lerp mix

layout (std140) uniform SceneData
{
	mat4 view;
	mat4 projection;
	mat4 viewInv;
	mat4 projectionInv;
	vec4 cameraPos;
	vec4 lightDir;
	vec3 lightColor;
	float lightIntensity;
	vec4 ambientColor;
	vec3 lightPosition;
	float padding; // implicit padding to align vec4
	
	vec4 secondaryLightDir;
	vec3 secondaryLightColor;
	float secondaryLightIntensity;

	float fogStart;
	float fogEnd;
	float fogEnabled;
	float padding4;
	vec4 fogColor;
	
	float screenWidth;
	float screenHeight;
	float time;
	float zNear;
	float zFar;
	float padding3[3];
};

// Voronoi

vec2 noise_randomVector(vec2 UV, float offset)
{
	mat2 m = mat2(15.27, 47.63, 99.41, 89.98);
	UV = fract(sin(UV * m) * 46839.32);
	return vec2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

void Voronoi(vec2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
	vec2 g = floor(UV * CellDensity);
	vec2 f = fract(UV * CellDensity);
	float t = 8.0;
	vec3 res = vec3(8.0, 0.0, 0.0);

	for(int y=-1; y<=1; y++)
	{
		for(int x=-1; x<=1; x++)
		{
			vec2 lattice = vec2(x,y);
			vec2 offset = noise_randomVector(lattice + g, AngleOffset);
			float d = distance(lattice + offset, f);
			if(d < res.x)
			{
				res = vec3(d, offset.x, offset.y);
				Out = res.x;
				Cells = res.y;
			}
		}
	}
}

// Noise

float NoiseRandomValue(vec2 uv)
{
	return fract(sin(dot(uv, vec2(12.9898, 78.233)))*43758.5453);
}

float NoiseInterpolate(float a, float b, float t)
{
	return (1.0-t)*a + (t*b);
}

float ValueNoise(vec2 uv)
{
	vec2 i = floor(uv);
	vec2 f = fract(uv);
	f = f * f * (3.0 - 2.0 * f);

	uv = abs(fract(uv) - 0.5);
	vec2 c0 = i + vec2(0.0, 0.0);
	vec2 c1 = i + vec2(1.0, 0.0);
	vec2 c2 = i + vec2(0.0, 1.0);
	vec2 c3 = i + vec2(1.0, 1.0);
	float r0 = NoiseRandomValue(c0);
	float r1 = NoiseRandomValue(c1);
	float r2 = NoiseRandomValue(c2);
	float r3 = NoiseRandomValue(c3);

	float bottomOfGrid = NoiseInterpolate(r0, r1, f.x);
	float topOfGrid = NoiseInterpolate(r2, r3, f.x);
	float t = NoiseInterpolate(bottomOfGrid, topOfGrid, f.y);
	return t;
}

float Noise(vec2 UV, float Scale)
{
	float t = 0.0;

	float freq = pow(2.0, float(0));
	float amp = pow(0.5, float(3-0));
	t += ValueNoise(vec2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

	freq = pow(2.0, float(1));
	amp = pow(0.5, float(3-1));
	t += ValueNoise(vec2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

	freq = pow(2.0, float(2));
	amp = pow(0.5, float(3-2));
	t += ValueNoise(vec2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

	return t;
}


#ifdef VERTEX_SHADER
layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec2 uv1;
layout (location = 3) in vec2 uv2;
layout (location = 4) in vec4 color;
layout (location = 5) in vec4 color2;

layout (std140) uniform ObjectData
{
#ifdef Instancing
	mat4 _model;
	mat4 _inverseModel;
	uint _objectIndex;
#else
	mat4 model;
	mat4 inverseModel;
	uint objectIndex;
#endif
};

#ifdef Instancing
uniform samplerBuffer InstancingModels;
uniform samplerBuffer InstancingInverseModels;
#endif


#ifdef Instancing
#define VERTEX_SETUP_INSTANCING mat4 model = mat4(texelFetch(InstancingModels, gl_InstanceID * 4), texelFetch(InstancingModels, gl_InstanceID * 4 + 1), texelFetch(InstancingModels, gl_InstanceID * 4 + 2), texelFetch(InstancingModels, gl_InstanceID * 4 + 3)); mat4 inverseModel = mat4(texelFetch(InstancingInverseModels, gl_InstanceID * 4), texelFetch(InstancingInverseModels, gl_InstanceID * 4 + 1), texelFetch(InstancingInverseModels, gl_InstanceID * 4 + 2), texelFetch(InstancingInverseModels, gl_InstanceID * 4 + 3)); 
#else
#define VERTEX_SETUP_INSTANCING ;
#endif


#endif

#ifdef PIXEL_SHADER

layout (std140) uniform ObjectData
{
#ifdef Instancing
	mat4 _model;
	mat4 _inverseModel;
	uint _objectIndex;
#else
	mat4 model;
	mat4 inverseModel;
	uint objectIndex;
#endif
};

#ifdef Instancing
uniform usamplerBuffer ObjectIndices;
#endif

#ifdef Instancing
#define PIXEL_SETUP_INSTANCING(T) uint objectIndex = uint(texelFetch(ObjectIndices, T).r); 
#else
#define PIXEL_SETUP_INSTANCING(T) ;
#endif


#define ddx(v) dFdx(v)
#define ddy(v) dFdy(v)

float LinearEyeDepth(float d)
{
	return zNear * zFar / (zFar + d * (zNear - zFar));
}

float Linear01Depth(float d)
{
	return (LinearEyeDepth(d) - zNear) / (zFar - zNear);
}

vec3 NormalBlend(vec3 A, vec3 B)
{
	return normalize(vec3(A.rg + B.rg, A.b * B.b));
}

vec3 lighting(vec3 col, vec3 normal)
{
	float diff = max(dot(-normal, lightDir.xyz), 0.0);
	float diff2 = max(dot(-normal, secondaryLightDir.xyz), 0.0);
	vec3 diffuse = diff * lightColor * lightIntensity + diff2 * secondaryLightColor * secondaryLightIntensity;
	vec3 ambient = ambientColor.rgb;
	return col * min(diffuse + ambient, vec3(1.2));
}

vec4 ApplyFog(vec4 color, vec3 worldPosition)
{
	float d = distance(cameraPos.xyz, worldPosition);

	float fogFactor = clamp((d - fogStart) / (fogEnd - fogStart), 0.0, 1.0);

	fogFactor = mix(0, fogFactor, fogEnabled);
	
	return mix(color, fogColor, fogFactor);
}
#endif