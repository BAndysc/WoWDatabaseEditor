using System;
using System.Numerics;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Managers
{
    public partial class LightManager : ILightManager, IDisposable
    {
        private readonly Engine engine;

        // Resolved during GatherLights: the first up-to-two directional Light entities in the scene
        // become the primary (sun, drives ambient + shadows) and secondary (fill) directional lights.
        public DirectionalLightData MainDirectional => mainDirectional;
        public DirectionalLightData SecondaryDirectional => secondaryDirectional;
        private DirectionalLightData mainDirectional;
        private DirectionalLightData secondaryDirectional;

        private readonly Archetype lights;

        private GpuPointLight[] gpuPointLights = new GpuPointLight[Constants.FORWARD_PLUS_MAX_LIGHTS];

        internal LightManager(Engine engine)
        {
            this.engine = engine;

            lights = engine.entityManager.NewArchetype()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<Light>();
        }

        /// <summary>
        /// Scans every (LocalToWorld, Light) entity once: point lights are packed into
        /// <see cref="gpuPointLights"/> for the current frame's Forward+ Light SSBO (returns the
        /// count written, capped at FORWARD_PLUS_MAX_LIGHTS), and the first up-to-two directional
        /// lights are resolved into <see cref="MainDirectional"/> / <see cref="SecondaryDirectional"/>
        /// (direction baked from the entity's LocalToWorld forward).
        /// </summary>
        private partial struct GatherLightsJob : IJob
        {
            private IChunkDataIterator itr;
            private ComponentDataAccess<LocalToWorld> transforms;
            private ComponentDataAccess<Light> lightComps;

            public GpuPointLight[] gpuPointLights;
            public int count;
            public int directionalCount;
            public DirectionalLightData mainDirectional;
            public DirectionalLightData secondaryDirectional;

            public void Execute(int start, int end)
            {
                for (int i = start; i < end; i++)
                {
                    ref readonly var transform = ref transforms[i];
                    ref readonly var light = ref lightComps[i];
                    if (light.Disabled)
                        continue;

                    if (light.Type == LightType.Directional)
                    {
                        if (directionalCount >= 2)
                            continue;
                        var dir = Vector3.TransformNormal(Vectors.Forward, transform.Matrix);
                        var len = dir.Length();
                        dir = len > 1e-6f ? dir / len : Vectors.Down;
                        var data = new DirectionalLightData
                        {
                            Exists = true,
                            Direction = dir,
                            Color = light.Color.XYZ(),
                            Intensity = light.Intensity,
                            AmbientColor = light.AmbientColor,
                            CastShadows = light.CastShadows,
                        };
                        if (directionalCount == 0)
                            mainDirectional = data;
                        else
                            secondaryDirectional = data;
                        directionalCount++;
                    }
                    else // point light
                    {
                        if (count >= gpuPointLights.Length)
                            continue;
                        gpuPointLights[count++] = new GpuPointLight
                        {
                            PositionRange = new Vector4(transform.Position, light.AttenuationEnd),
                            Color = new Vector4(light.Color.XYZ(), light.Intensity),
                            Params = new Vector4(light.AttenuationStart, 0, 0, 0),
                        };
                    }
                }
            }
        }

        internal int GatherLights(out GpuPointLight[] lights)
        {
            lights = gpuPointLights;
            var job = new GatherLightsJob { gpuPointLights = gpuPointLights };
            job.Run(this.lights);
            mainDirectional = job.mainDirectional;
            secondaryDirectional = job.secondaryDirectional;
            return job.count;
        }

        public void Dispose()
        {

        }
    }
}
