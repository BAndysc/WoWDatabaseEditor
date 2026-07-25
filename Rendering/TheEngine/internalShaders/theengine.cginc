#extension GL_EXT_nonuniform_qualifier : require

#define PI 3.14159265358979323846
#define lerp mix

// Cascaded shadow maps - must match Constants.SHADOW_CASCADE_COUNT and the fixed-size cascade
// arrays in SceneBuffer.cs. The shadow-map resolution arrives via the shadowMapResolution uniform.
#define NUM_CASCADES 4

// Native Vulkan conventions: the projection (System.Numerics CreatePerspectiveFieldOfView) is
// right-handed and already emits clip z in [0, w] (ndc z in [0, 1]), so vertex shaders write
// gl_Position directly - no VK_CLIP remap. Render targets are stored top-down (row 0 == top of
// screen) via a negative-height viewport, so no present/screenshot Y-flip.
layout (std140, set = 0, binding = 0) uniform SceneData
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
	int lightCount;
	int tilesX;
	int tilesY;
	int decalCount;
	// 0 = main view's Light/DecalGrid+IndexList SSBOs, 1 = the scene view's own independent set
	// (see Constants.SCENE_*_BINDING) - lets the same lighting()/ApplyDecals() code serve both
	// the game view and the editor's scene view, each tile-culled against its own camera.
	int gridSet;
	// explicit padding so cascadeViewProj lands on the std140 16-byte boundary the GPU expects
	// (gridSet ends at byte 440 -> matrices at 448); mirrors GridSetPad0/1 in SceneBuffer.cs.
	int gridSetPad0;
	int gridSetPad1;

	// Cascaded shadow maps - mirrors SceneBuffer.cs (must match field order/layout). cascadeSplits
	// holds each cascade's far bound as a positive view-space distance; cascadeTextureIndices are the
	// packed bindless indices of the cascade depth textures; cascadeCount == 0 disables shadowing.
	mat4 cascadeViewProj[NUM_CASCADES];
	vec4 cascadeSplits;
	ivec4 cascadeTextureIndices;
	int cascadeCount;
	// configurable shadow sampling params (from the CascadeShadowMap component); the three floats
	// also complete the std140 16-byte block after cascadeCount. shadowMapResolution feeds the PCF
	// texel step; shadowNormalBias/shadowConstantBias bias the depth compare.
	float shadowMapResolution;
	float shadowNormalBias;
	float shadowConstantBias;
	// PCF blur + cascade-blend controls (own std140 16-byte block, padded). shadowPcfRadius = kernel
	// half-size, shadowBlur = per-tap texel spacing, shadowCascadeBlend = cross-fade band fraction.
	int shadowPcfRadius;
	float shadowBlur;
	float shadowCascadeBlend;
	int shadowPad1;
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
// invariant so the depth-prepass shader variant and the forward variant compute bit-identical
// clip depth - the opaque pass tests depth with Equal against the prepass (see ObjectDrawRenderStage).
invariant gl_Position;
layout (location = 0) in vec3 position;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec2 uv1;
layout (location = 3) in vec2 uv2;
layout (location = 4) in vec4 color;
layout (location = 5) in vec4 color2;

// Per-instance object data lives in std430 SSBOs (set 1), indexed by gl_InstanceIndex.
// Every draw (single or batched) goes through this path: a non-batched draw simply has
// instanceCount=1/firstInstance=0, so gl_InstanceIndex is 0 and reads slot 0.
layout(std430, set = 1, binding = 12) readonly buffer InstancingModels { mat4 models[]; };
layout(std430, set = 1, binding = 13) readonly buffer InstancingInverseModels { mat4 inverseModels[]; };
layout(std430, set = 1, binding = 14) readonly buffer InstancingDrawData { ivec4 drawDataArray[]; };

// gl_InstanceIndex is vertex-stage only, so it's passed to the fragment stage via this
// flat varying for MATERIAL_INDEX (see PIXEL_SHADER section below).
layout(location = 12) flat out int vMatIdx;

#define VERTEX_SETUP_INSTANCING mat4 model = models[gl_InstanceIndex]; mat4 inverseModel = inverseModels[gl_InstanceIndex]; ivec4 drawDataVector = drawDataArray[gl_InstanceIndex]; int drawDataX = drawDataVector.x; int drawDataY = drawDataVector.y; int drawDataZ = drawDataVector.z; int drawDataW = drawDataVector.w; vMatIdx = gl_InstanceIndex;

#endif

// Forward+ tiled light culling: GPU-side mirrors of Structures/GpuPointLight.cs and the
// LightGrid/LightIndexList layouts (see VulkanRenderBackend, set 0 bindings 2-4). Must
// match TheAvaloniaOpenGL.Constants.FORWARD_PLUS_*.
#define FORWARD_PLUS_TILE_SIZE 16
#define FORWARD_PLUS_MAX_LIGHTS_PER_TILE 64
// Must match TheAvaloniaOpenGL.Constants.FORWARD_PLUS_MAX_TILES_X/Y/GRID_SLOTS: the per-tile
// grid/index buffers are allocated GRID_SLOTS * MAX_TILES_X * MAX_TILES_Y elements large (slot 0
// = main view, slot 1 = editor scene view - see SceneData.gridSet), so the two views' Forward+
// culling results can coexist within the same frame without exceeding this device's
// maxPerStageDescriptorStorageBuffers limit by adding a second set of SSBO bindings.
#define FORWARD_PLUS_MAX_TILES_X 256
#define FORWARD_PLUS_MAX_TILES_Y 256
#define FORWARD_PLUS_GRID_SLOT_TILES (FORWARD_PLUS_MAX_TILES_X * FORWARD_PLUS_MAX_TILES_Y)

struct GpuPointLight
{
	vec4 positionRange; // xyz = world position, w = attenuation end (range)
	vec4 color;         // rgb = color, a = intensity
	vec4 params;        // x = attenuation start
};

struct LightGridEntry
{
	uint offset;
	uint count;
};

// Forward+ tiled decal culling: GPU-side mirror of Structures/GpuDecal.cs and the
// DecalGrid/DecalIndexList layouts (see VulkanRenderBackend, set 0 bindings 5-7). Must
// match TheAvaloniaOpenGL.Constants.FORWARD_PLUS_MAX_DECALS*.
#define FORWARD_PLUS_MAX_DECALS_PER_TILE 64

struct GpuDecal
{
	mat4 worldToLocal;
	vec4 positionRadius;
	vec4 color;
	int albedoTextureIndex;
	float fadeAngleCos;
	float sortBias;
	uint pickId;
};

struct DecalGridEntry
{
	uint offset;
	uint count;
};

#if defined(PIXEL_SHADER) || defined(COMPUTE_SHADER)

// Bindless texture arrays (set 2): every sampled texture/sampler is registered into a
// slot here once, and materials reference textures by a packed int index instead of a
// per-draw descriptor. SampledImage and Sampler are split into separate arrays (sizes
// must match VulkanRenderBackend.BindlessTextureCapacity/BindlessSamplerCapacity) since
// MoltenVK caps Samplers at 1024 but SampledImages at ~1e6.
layout(set = 2, binding = 0) uniform texture2D bindlessTextures[16384];
layout(set = 2, binding = 1) uniform sampler bindlessSamplers[128];

// packed index = (samplerSlot << 20) | textureSlot, see VulkanRenderBackend.TextureIndexBits.
// The slots are clamped into the array bounds: a missing/garbage texture index (e.g. -1 for an unset
// texture -> (uint)-1 & 0xFFFFF == 1048575, far past the 16384-entry array) is otherwise a hard GPU
// page fault on MoltenVK (no robustness for descriptor-array indexing). .length() tracks the declared sizes.
#define BINDLESS_TEX_SLOT(idx) nonuniformEXT(min(uint(idx) & 0xFFFFFu, uint(bindlessTextures.length()) - 1u))
#define BINDLESS_SMP_SLOT(idx) nonuniformEXT(min(uint(idx) >> 20u, uint(bindlessSamplers.length()) - 1u))
#define SAMPLE_BINDLESS(idx, uv) texture(sampler2D(bindlessTextures[BINDLESS_TEX_SLOT(idx)], bindlessSamplers[BINDLESS_SMP_SLOT(idx)]), uv)
// texelFetch form (no filtering/wrapping) - used by the light-cull compute pass to read the depth texture
#define SAMPLE_BINDLESS_TEXEL(idx, coord) texelFetch(sampler2D(bindlessTextures[BINDLESS_TEX_SLOT(idx)], bindlessSamplers[BINDLESS_SMP_SLOT(idx)]), coord, 0).r

// populated by light_cull.comp, consumed by lighting() below. The fragment stage only
// reads LightGrid/LightIndexList - MoltenVK requires NonWritable (readonly) on fragment
// storage buffers unless fragmentStoresAndAtomics is enabled, so only the compute pass
// (which writes them) declares them read-write.
layout(std430, set = 0, binding = 2) readonly buffer LightBuffer { GpuPointLight lights[]; };
#ifdef PIXEL_SHADER
layout(std430, set = 0, binding = 3) readonly buffer LightGridBuffer { LightGridEntry lightGrid[]; };
layout(std430, set = 0, binding = 4) readonly buffer LightIndexBuffer { uint lightIndices[]; };
#else
layout(std430, set = 0, binding = 3) buffer LightGridBuffer { LightGridEntry lightGrid[]; };
layout(std430, set = 0, binding = 4) buffer LightIndexBuffer { uint lightIndices[]; };
#endif

// populated by tile_cull.comp, consumed by ApplyDecals() below. Same readonly-in-fragment /
// read-write-in-compute split as the light buffers above.
layout(std430, set = 0, binding = 5) readonly buffer DecalBuffer { GpuDecal decals[]; };
#ifdef PIXEL_SHADER
layout(std430, set = 0, binding = 6) readonly buffer DecalGridBuffer { DecalGridEntry decalGrid[]; };
layout(std430, set = 0, binding = 7) readonly buffer DecalIndexBuffer { uint decalIndices[]; };
#else
layout(std430, set = 0, binding = 6) buffer DecalGridBuffer { DecalGridEntry decalGrid[]; };
layout(std430, set = 0, binding = 7) buffer DecalIndexBuffer { uint decalIndices[]; };
#endif

// Positive view-space eye Z (distance along the view axis) for a stored Vulkan depth value
// d in [0,1]. Reconstructed via projectionInv so it matches view-space Z (-(view*pos).z)
// EXACTLY. The textbook closed form (zNear*zFar / (zFar + d*(zNear-zFar))) does NOT match
// this engine's projection and mis-scaled the Forward+ depth slab against the light's
// view-space Z (per-tile staircase artifacts). The projection emits native [0,1] ndc z, so
// the stored depth IS the ndc z projectionInv expects - feed it directly. Eye Z depends only
// on ndc z for a perspective proj, so x/y = 0 is fine.
float LinearEyeDepth(float d)
{
	vec4 eye = projectionInv * vec4(0.0, 0.0, d, 1.0);
	return -eye.z / eye.w;
}

float Linear01Depth(float d)
{
	return (LinearEyeDepth(d) - zNear) / (zFar - zNear);
}

#endif

#ifdef PIXEL_SHADER

layout(std430, set = 1, binding = 15) readonly buffer InstancingObjectIndices { uint objectIndices[]; };
layout(std430, set = 1, binding = 14) readonly buffer InstancingDrawData { ivec4 drawDataArray[]; };

// Per-instance index into a material type's MaterialDataArray SSBO (set=1 binding=0, declared
// per-shader, see MaterialManager.MaterialTypeArray<T>) - lets a converted shader's MaterialData
// struct be a row in a per-type array indexed by gl_InstanceIndex instead of a per-draw UBO.
layout(std430, set = 1, binding = 16) readonly buffer InstancingMaterialIndex { int materialIndices[]; };

// gl_InstanceIndex isn't available in the fragment stage - vMatIdx carries it over from
// VERTEX_SETUP_INSTANCING via a flat varying (location = 12).
layout(location = 12) flat in int vMatIdx;
// max(...,0): a renderer whose material was never registered keeps Material.MaterialArrayIndex == -1
// (e.g. a model with 0 materials like a particle-emitter doodad). Indexing materials[-1] is a negative
// out-of-bounds SSBO read - a GPU page fault on MoltenVK, which does NOT clamp storage-buffer reads.
// Clamp to 0 so a stray invalid index can never fault; such renderers should also be skipped CPU-side.
#define MATERIAL_INDEX max(materialIndices[vMatIdx], 0)

#define PIXEL_SETUP_INSTANCING(T) uint objectIndex = objectIndices[T]; ivec4 drawDataVector = drawDataArray[T]; int drawDataX = drawDataVector.x; int drawDataY = drawDataVector.y; int drawDataZ = drawDataVector.z; int drawDataW = drawDataVector.w;


#define ddx(v) dFdx(v)
#define ddy(v) dFdy(v)

vec3 NormalBlend(vec3 A, vec3 B)
{
	return normalize(vec3(A.rg + B.rg, A.b * B.b));
}

// Albedo-only Forward+ decal blending: looks up the current fragment's tile in DecalGrid and
// alpha-blends every decal whose box actually contains worldPos (exact OBB test - the cull
// compute pass only did a conservative sphere pre-test). `pickId` is the GPU-side half of decal
// picking (see RenderManager.PickObject / DecalManager.PickIdBase): later entries paint over
// earlier ones (same order the alpha blend composites in), so the last decal whose alpha clears
// the threshold is both what's visually on top and what gets picked.
vec3 ApplyDecals(vec3 albedo, vec3 normal, vec3 worldPos, out uint pickId)
{
	pickId = 0u;
	if (tilesX <= 0 || tilesY <= 0)
		return albedo;

	ivec2 tile = clamp(ivec2(gl_FragCoord.xy) / FORWARD_PLUS_TILE_SIZE, ivec2(0), ivec2(tilesX - 1, tilesY - 1));
	// gridSet selects which GRID_SLOT_TILES-sized half of decalGrid/decalIndices this view's
	// results live in (slot 0 = main view, slot 1 = scene view) - entry.offset, written by
	// tile_cull.comp using this same offset tile index, is already absolute into that half.
	uint decalTileIndex = uint(tile.y * tilesX + tile.x) + uint(gridSet) * uint(FORWARD_PLUS_GRID_SLOT_TILES);
	DecalGridEntry entry = decalGrid[decalTileIndex];
	for (uint i = 0u; i < entry.count; i++)
	{
		GpuDecal decal = decals[decalIndices[entry.offset + i]];

		vec4 localPos = decal.worldToLocal * vec4(worldPos, 1.0);
		if (any(greaterThan(abs(localPos.xyz), vec3(1.0))))
			continue; // exact OBB test

		vec3 projAxis = normalize(vec3(decal.worldToLocal[0].z, decal.worldToLocal[1].z, decal.worldToLocal[2].z));
		float facing = abs(dot(projAxis, normal));
		if (facing < decal.fadeAngleCos)
			continue; // near-perpendicular surface - avoid projection stretching

		vec2 uv = localPos.xy * 0.5 + 0.5;
		vec4 decalSample = SAMPLE_BINDLESS(decal.albedoTextureIndex, uv);
		float edgeFade = smoothstep(decal.fadeAngleCos, 1.0, facing);
		float alpha = clamp(decalSample.a * decal.color.a * edgeFade, 0.0, 1.0);
		albedo = mix(albedo, decalSample.rgb * decal.color.rgb, alpha);
		if (alpha > 0.05)
			pickId = decal.pickId;
	}
	return albedo;
}

// PCF-filtered lit fraction [0,1] for ONE cascade. Returns 1.0 (fully lit) when the biased point
// falls outside this cascade's depth map. `biasedPos` is the already normal-offset world position.
// Manual (2r+1)^2 PCF over the bindless cascade depth texture - no Vulkan comparison sampler needed;
// the projection emits native [0,1] ndc z, so the light-space depth compares directly to the stored.
float SampleCascadeLit(int cascade, vec3 biasedPos, float constBias)
{
	int texIndex = cascadeTextureIndices[cascade];
	if (texIndex < 0)
		return 1.0;

	vec4 lightClip = cascadeViewProj[cascade] * vec4(biasedPos, 1.0);
	if (lightClip.w <= 0.0)
		return 1.0;
	vec3 proj = lightClip.xyz / lightClip.w;
	// the cascade depth map is rendered with the engine's negative-height viewport (stored
	// top-down, row 0 == top), so sampling it from light-space NDC needs the same Y flip as
	// every other render-target reconstruct: uv.y = 0.5 - 0.5*ndc.y (see ssao.frag). Without
	// this the shadow lookup is mirrored vertically and the cast shadows land in the wrong place.
	vec2 uv = vec2(proj.x * 0.5 + 0.5, 0.5 - proj.y * 0.5);
	float fragDepth = proj.z;
	if (uv.x <= 0.0 || uv.x >= 1.0 || uv.y <= 0.0 || uv.y >= 1.0 || fragDepth > 1.0 || fragDepth < 0.0)
		return 1.0;

	// PCF blur: shadowPcfRadius is the kernel half-size (taps = (2r+1)^2, r=0 -> hard shadows),
	// shadowBlur scales the per-tap spacing in texels (wider = softer penumbra).
	int radius = clamp(shadowPcfRadius, 0, 4);
	float texel = max(shadowBlur, 0.0) / max(shadowMapResolution, 1.0);
	float lit = 0.0;
	float total = 0.0;
	for (int y = -radius; y <= radius; ++y)
	{
		for (int x = -radius; x <= radius; ++x)
		{
			float stored = SAMPLE_BINDLESS(texIndex, uv + vec2(float(x), float(y)) * texel).r;
			lit += (fragDepth - constBias > stored) ? 0.0 : 1.0;
			total += 1.0;
		}
	}
	return lit / total;
}

// Cascaded shadow factor for the main directional light. `normal` is the outward geometric normal
// (lighting() computes N.L as dot(-normal, lightDir), so the surface faces the light when
// dot(normal, -lightDir) > 0). Returns 1.0 (fully lit) when shadows are disabled or beyond the last
// split. To hide the hard seam where cascades meet, the last shadowCascadeBlend fraction of each
// cascade's range cross-fades into the next (larger) cascade.
float SampleCascadedShadow(vec3 worldPos, vec3 normal)
{
	if (cascadeCount <= 0)
		return 1.0;

	// select the tightest cascade whose far bound still contains this fragment (view-space distance)
	float viewDepth = -(view * vec4(worldPos, 1.0)).z;
	int cascade = cascadeCount - 1;
	for (int i = 0; i < cascadeCount; ++i)
	{
		if (viewDepth < cascadeSplits[i])
		{
			cascade = i;
			break;
		}
	}
	if (viewDepth >= cascadeSplits[cascadeCount - 1])
		return 1.0; // beyond the shadow distance

	// normal-offset bias: push the sample point out along the surface normal, more on grazing
	// surfaces, to keep self-shadowing acne off lit faces. shadowNormalBias is configurable; the
	// base term keeps a small offset on face-on surfaces (proportional, so one slider tunes both).
	float ndotl = max(dot(normal, -lightDir.xyz), 0.0);
	float slope = clamp(1.0 - ndotl, 0.0, 1.0);
	vec3 biasedPos = worldPos + normal * (shadowNormalBias * (slope + 0.15));
	float constBias = shadowConstantBias;

	float lit = SampleCascadeLit(cascade, biasedPos, constBias);

	// cross-fade into the next cascade across a band at this cascade's far edge so the resolution
	// jump isn't a visible line. The band is shadowCascadeBlend of the cascade's own [near,far] range.
	float fade = clamp(shadowCascadeBlend, 0.0, 0.5);
	if (fade > 0.0 && cascade < cascadeCount - 1)
	{
		float nearSplit = cascade == 0 ? 0.0 : cascadeSplits[cascade - 1];
		float farSplit = cascadeSplits[cascade];
		float bandStart = farSplit - fade * (farSplit - nearSplit);
		if (viewDepth > bandStart)
		{
			float t = clamp((viewDepth - bandStart) / max(farSplit - bandStart, 1e-4), 0.0, 1.0);
			lit = mix(lit, SampleCascadeLit(cascade + 1, biasedPos, constBias), t);
		}
	}
	return lit;
}

vec3 lighting(vec3 col, vec3 normal, vec3 worldPos)
{
	float diff = max(dot(-normal, lightDir.xyz), 0.0);
	float diff2 = max(dot(-normal, secondaryLightDir.xyz), 0.0);
	float shadow = SampleCascadedShadow(worldPos, normal);
	vec3 diffuse = diff * shadow * lightColor * lightIntensity + diff2 * secondaryLightColor * secondaryLightIntensity;

	if (tilesX > 0 && tilesY > 0)
	{
		ivec2 tile = clamp(ivec2(gl_FragCoord.xy) / FORWARD_PLUS_TILE_SIZE, ivec2(0), ivec2(tilesX - 1, tilesY - 1));
		// gridSet selects which GRID_SLOT_TILES-sized half of lightGrid/lightIndices this view's
		// results live in (slot 0 = main view, slot 1 = scene view) - entry.offset, written by
		// tile_cull.comp using this same offset tile index, is already absolute into that half.
		uint lightTileIndex = uint(tile.y * tilesX + tile.x) + uint(gridSet) * uint(FORWARD_PLUS_GRID_SLOT_TILES);
		LightGridEntry entry = lightGrid[lightTileIndex];
		for (uint i = 0u; i < entry.count; i++)
		{
			GpuPointLight light = lights[lightIndices[entry.offset + i]];

			vec3 toLight = light.positionRange.xyz - worldPos;
			float dist = length(toLight);
			float range = light.positionRange.w;
			if (dist >= range)
				continue;

			vec3 lightDirN = toLight / max(dist, 1e-4);
			float ndotl = max(dot(normal, lightDirN), 0.0);
			float atten = 1.0 - clamp((dist - light.params.x) / max(range - light.params.x, 1e-4), 0.0, 1.0);
			atten *= atten;

			diffuse += ndotl * atten * light.color.rgb * light.color.a;
		}
	}

	vec3 ambient = ambientColor.rgb;
	// allow strong directional light to over-brighten past 1.0 so sun-lit surfaces blow out to white
	// in the LDR target (a "blinding sun" look); normal intensity-1 lighting never reaches this cap.
	return col * min(diffuse + ambient, vec3(3.0));
}

vec4 ApplyFog(vec4 color, vec3 worldPosition)
{
	float d = distance(cameraPos.xyz, worldPosition);

	float fogFactor = clamp((d - fogStart) / (fogEnd - fogStart), 0.0, 1.0);

	fogFactor = mix(0.0, fogFactor, fogEnabled);

	return mix(color, fogColor, fogFactor);
}
#endif
