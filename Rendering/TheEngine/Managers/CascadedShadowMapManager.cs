using System;
using System.Numerics;
using TheEngine;
using TheEngine.Resources;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
    /// <summary>
    /// Owns the directional-light cascaded shadow map resources and the per-frame cascade fitting
    /// math. The depth maps are shared between the game view and the editor scene view: the
    /// orchestrator (<see cref="RenderManager"/>) calls <see cref="ComputeCascades"/> then renders
    /// each cascade's depth before that view's opaque pass, so the (serialized) views never read a
    /// cascade another view is still writing.
    ///
    /// There is no global shadow toggle: shadows render only while an enabled <see cref="CascadeShadowMap"/>
    /// entity exists (resolved by <see cref="TryResolveSettings"/>), and every tunable (cascade
    /// distances, resolution, biases) comes from that component.
    ///
    /// Conventions: this engine is right-handed, Z-up (<see cref="Vectors.Up"/> == +Z) with native
    /// Vulkan [0,1] depth (same matrix family as the camera, see theengine.cginc). World->clip is
    /// built row-vector as <c>view * proj</c> (matching <c>camera.ViewMatrix * camera.ProjectionMatrix</c>).
    /// </summary>
    internal partial class CascadedShadowMapManager : IDisposable
    {
        public const int CascadeCount = Constants.SHADOW_CASCADE_COUNT;

        private readonly Engine engine;
        private readonly Archetype settingsArchetype;

        private readonly ITexture[] cascadeTextures = new ITexture[CascadeCount];
        private readonly ITexture[] cascadeRenderTargets = new ITexture[CascadeCount];
        private readonly int[] bindlessIndices = new int[CascadeCount];
        private int currentResolution; // 0 until the textures are first created

        // Results of the most recent ComputeCascades call (valid for one camera at a time).
        public readonly Matrix[] LightView = new Matrix[CascadeCount];
        public readonly Matrix[] LightProj = new Matrix[CascadeCount];
        public readonly Matrix[] LightViewProj = new Matrix[CascadeCount]; // world->clip (view * proj)
        public readonly float[] SplitDistances = new float[CascadeCount];  // view-space far bound per cascade

        public CascadedShadowMapManager(Engine engine)
        {
            this.engine = engine;
            settingsArchetype = engine.entityManager.NewArchetype()
                .WithComponentData<CascadeShadowMap>();
        }

        /// <summary>Finds the active shadow configuration: the first enabled <see cref="CascadeShadowMap"/>
        /// entity. Returns false (and shadows are skipped) when none exists.</summary>
        private partial struct FindSettings : IJob
        {
            private IChunkDataIterator itr;
            private ComponentDataAccess<CascadeShadowMap> shadows;

            public bool any;
            public CascadeShadowMap found;

            public void Execute(int start, int end)
            {
                if (any)
                    return;
                for (int i = start; i < end; i++)
                {
                    if (shadows[i].Disabled)
                        continue;
                    found = shadows[i];
                    any = true;
                    return;
                }
            }
        }
        public bool TryResolveSettings(out CascadeShadowMap settings)
        {
            var job = new FindSettings();
            job.Run(settingsArchetype);
            settings = job.found;
            return job.any;
        }

        /// <summary>Resolution actually backing the cascade textures this frame (clamped). Feeds the
        /// shader's PCF texel step via the scene buffer.</summary>
        public int Resolution => currentResolution;

        /// <summary>(Re)creates the cascade depth textures at the requested resolution, registering
        /// their bindless indices. A no-op when the resolution is unchanged; safe to call every frame.</summary>
        public void EnsureResources(int resolution)
        {
            resolution = Math.Clamp(resolution, 256, 8192);
            if (currentResolution == resolution)
                return;

            DisposeTextures();
            for (int i = 0; i < CascadeCount; i++)
            {
                cascadeTextures[i] = engine.textureManager.CreateTexture(null, resolution, resolution, TextureFormat.DepthComponent);
                cascadeRenderTargets[i] = engine.textureManager.CreateDepthOnlyRenderTexture(cascadeTextures[i]);
                bindlessIndices[i] = engine.textureManager.GetBindlessIndex(cascadeTextures[i]);
            }
            currentResolution = resolution;
        }

        public ITexture RenderTarget(int cascade) => cascadeRenderTargets[cascade];
        public ITexture DepthTexture(int cascade) => cascadeTextures[cascade];
        public int BindlessIndex(int cascade) => bindlessIndices[cascade];

        /// <summary>Fits one orthographic light frustum to each split of the camera's view frustum
        /// and fills the LightView/LightProj/LightViewProj/SplitDistances arrays. Uses bounding-sphere
        /// cascades with texel-grid snapping so the shadow edges don't shimmer as the camera moves.
        /// The cascade far bounds and back-extrusion come from the resolved <see cref="CascadeShadowMap"/>.</summary>
        public void ComputeCascades(ICamera camera, Vector3 lightDirection, in CascadeShadowMap settings)
        {
            Vector3 lightDir = Vector3.Normalize(lightDirection);

            Matrix camViewProj = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix.Invert(camViewProj, out Matrix invViewProj);

            float near = camera.NearClip;
            float shadowDistance = MathF.Max(settings.Split3, near + 1f);
            float far = MathF.Min(camera.FarClip, shadowDistance);

            // configured per-cascade far bounds, forced ascending inside (near, far]. The lower bound
            // is itself capped at far so it can never exceed the upper bound (which would throw): once
            // a split reaches far the remaining cascades collapse onto far rather than crash.
            Span<float> splits = stackalloc float[CascadeCount + 1];
            splits[0] = near;
            Span<float> configured = stackalloc float[CascadeCount] { settings.Split0, settings.Split1, settings.Split2, settings.Split3 };
            float prev = near;
            for (int i = 0; i < CascadeCount; i++)
            {
                float lo = MathF.Min(prev + 0.01f, far);
                float s = Math.Clamp(configured[i], lo, far);
                splits[i + 1] = s;
                prev = s;
            }

            // unproject the 8 frustum corners (NDC: x,y in {-1,1}; z=0 near, z=1 far - Vulkan [0,1]).
            // indices 0..3 are the near plane, 4..7 the far plane, matching x/y per (k, k+4).
            Span<Vector3> corners = stackalloc Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float nx = (i & 1) == 0 ? -1f : 1f;
                float ny = (i & 2) == 0 ? -1f : 1f;
                float nz = i < 4 ? 0f : 1f;
                Vector4 w = Vector4.Transform(new Vector4(nx, ny, nz, 1f), invViewProj);
                corners[i] = new Vector3(w.X, w.Y, w.Z) / w.W;
            }

            Vector3 up = MathF.Abs(Vector3.Dot(lightDir, Vectors.Up)) > 0.99f ? Vectors.Forward : Vectors.Up;
            float casterExtrusion = MathF.Max(settings.CasterExtrusion, 0f);

            // The frustum corners span the camera's FULL depth range [near, FarClip], so a point
            // corners[k] + edge*t sits at view-depth near + t*(FarClip - near). To place a slice
            // boundary at view-depth `s` we therefore divide by (FarClip - near), NOT by (far - near):
            // normalizing against the (smaller) shadow distance overshot every slice by a factor of
            // (FarClip-near)/(shadowDistance-near), ballooning all cascades (even cascade 0) and making
            // shadows blurrier the SHORTER the max distance was.
            float frustumSpan = MathF.Max(camera.FarClip - near, 1e-4f);

            Span<Vector3> slice = stackalloc Vector3[8];
            for (int c = 0; c < CascadeCount; c++)
            {
                float tNear = (splits[c] - near) / frustumSpan;
                float tFar = (splits[c + 1] - near) / frustumSpan;

                // slice corners: interpolate each frustum edge (near corner -> far corner) by the
                // linear view-space fraction (view-space Z varies linearly along a frustum edge).
                Vector3 center = Vector3.Zero;
                for (int k = 0; k < 4; k++)
                {
                    Vector3 edge = corners[k + 4] - corners[k];
                    Vector3 nearP = corners[k] + edge * tNear;
                    Vector3 farP = corners[k] + edge * tFar;
                    slice[k] = nearP;
                    slice[k + 4] = farP;
                    center += nearP + farP;
                }
                center /= 8f;

                float radius = 0f;
                for (int k = 0; k < 8; k++)
                    radius = MathF.Max(radius, (slice[k] - center).Length());
                radius = MathF.Ceiling(radius * 16f) / 16f;

                // texel-grid snap: quantize the cascade center in light space to whole shadow-map
                // texels so a fixed-size cascade slides in texel increments (no edge crawling).
                Matrix lightRot = Matrix.CreateLookAt(Vector3.Zero, lightDir, up);
                float texelsPerUnit = currentResolution / (radius * 2f);
                Vector3 centerLS = Vector3.Transform(center, lightRot);
                centerLS.X = MathF.Floor(centerLS.X * texelsPerUnit) / texelsPerUnit;
                centerLS.Y = MathF.Floor(centerLS.Y * texelsPerUnit) / texelsPerUnit;
                Matrix.Invert(lightRot, out Matrix lightRotInv);
                center = Vector3.Transform(centerLS, lightRotInv);

                Vector3 eye = center - lightDir * (radius + casterExtrusion);
                Matrix view = Matrix.CreateLookAt(eye, center, up);
                float depthRange = radius * 2f + casterExtrusion;
                Matrix proj = Matrix.CreateOrthographicOffCenter(-radius, radius, -radius, radius, 0f, depthRange);

                LightView[c] = view;
                LightProj[c] = proj;
                LightViewProj[c] = view * proj;
                SplitDistances[c] = splits[c + 1];
            }
        }

        private void DisposeTextures()
        {
            if (currentResolution == 0)
                return;
            for (int i = 0; i < CascadeCount; i++)
            {
                engine.textureManager.DisposeTexture(cascadeRenderTargets[i]);
                engine.textureManager.DisposeTexture(cascadeTextures[i]);
            }
            currentResolution = 0;
        }

        public void Dispose() => DisposeTextures();
    }
}
