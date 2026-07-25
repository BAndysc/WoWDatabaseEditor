using System.Runtime.InteropServices;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using Veldrid;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public class AnimatedM2Light : IManagedComponentData
    {
        public M2 M2 { get; set; }
        public M2Light M2Light { get; set; }
        public float _time;
    }

    public class LightingManager : System.IDisposable
    {
        private readonly IGameContext gameContext;
        private readonly IGameProperties gameProperties;
        private readonly IMeshManager meshManager;
        private readonly ITextureManager textureManager;
        private readonly IRenderManager renderManager;
        private readonly CameraManager cameraManager;
        private readonly LightStore lightStore;
        private readonly ILightManager lightManager;
        private readonly TimeManager timeManager;
        private readonly IEntityManager entityManager;
        // sun (primary, shadow-casting) and moon (secondary) directional lights, now ECS entities
        // whose direction is their LocalToWorld forward (driven each frame from the day/night cycle).
        private Entity sunEntity;
        private Entity moonEntity;
        private Entity shadowConfigEntity;
        private IMesh skySphereMesh;
        private Material<material_data_t> skyMaterial;
        private ITexture noiseTexture;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct material_data_t
        {
            public Vector4 top;
            public Vector4 middle;
            public Vector4 towardsHorizon;
            public Vector4 horizon;
            public Vector4 justAboveHorizon;
            public Vector4 sunColor;

            public Vector4 cloudsColor1;
            public float timeOfDay; // 0 - midnight, 0.5 - noon, 1 - midnight
            public float timeOfDayHalf; // 0 - noon, 0.5 - midnight, 1 - noon
            public float cloudsDensity;
            public int cloudsTexIndex; // bindless slot of the clouds noise texture (was padding)
        };

        public LightingManager(IGameContext gameContext,
            IGameProperties gameProperties,
            IMeshManager meshManager, 
            IMaterialManager materialManager,
            ITextureManager textureManager,
            IRenderManager renderManager,
            CameraManager cameraManager,
            LightStore lightStore,
            ILightManager lightManager,
            TimeManager timeManager,
            IEntityManager entityManager,
            IPipelineManager pipelineManager,
            IShaderManager shaderManager)
        {
            this.gameContext = gameContext;
            this.gameProperties = gameProperties;
            this.meshManager = meshManager;
            this.textureManager = textureManager;
            this.renderManager = renderManager;
            this.cameraManager = cameraManager;
            this.lightStore = lightStore;
            this.lightManager = lightManager;
            this.timeManager = timeManager;
            this.entityManager = entityManager;

            skySphereMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/skysphere.obj").MeshData);
            var skyPipeline = pipelineManager.CreatePipeline(shaderManager.LoadShader("data/skybox.json"), PrimitiveTopology.TriangleList, new GraphicsPipelineDescription()
            {
                RasterizerState = RasterizerStateDescription.CullNone,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqualRead,
                BlendState = BlendStateDescription.SingleDisabled
            }, false);
            skyMaterial = materialManager.CreateMaterial<material_data_t>(skyPipeline);
            // noiseTexture is held by this field for its whole lifetime, so its bindless slot stays
            // valid without a per-material texture binding (see cloudsTexIndex in material_data_t).
            noiseTexture = textureManager.LoadTexture("textures/noise_512.png");
        }

        public void Dispose()
        {
            meshManager.DisposeMesh(skySphereMesh);
            textureManager.DisposeTexture(noiseTexture);
        }

        public CombinedLight? BestLight { get; private set; }
        private DbcLight? destLight = null;
        private DbcLight? previousLight = null;
        private float t = 0;

        private float cachedDelta;

        public void Update(float delta)
        {
            cachedDelta = delta;
            var position = cameraManager.Position;
            var bestDistance = float.MaxValue;
            DbcLight? newLight = null;
            foreach (var lightning in lightStore)
            {
                if (lightning.Continent != gameContext.CurrentMapId)
                    continue;
                var pos = new Vector3(lightning.X, lightning.Y, lightning.Z);
                var distance = (pos - position).Length();
                //if (distance < lightning.FalloffEnd)
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        newLight = lightning;
                    }
                }
            }

            if (newLight != null)
            {
                if (previousLight == null)
                {
                    previousLight = newLight;
                    destLight = newLight;
                    BestLight = new CombinedLight(previousLight, destLight);
                }
                else if (destLight != newLight)
                {
                    previousLight = destLight;
                    destLight = newLight;
                    t = 0;
                    BestLight = new CombinedLight(previousLight, destLight);
                }
            }
            if (BestLight != null)
            {
                t += delta / 1000;
                BestLight.Mix = Math.Clamp(t, 0, 1);
            }
            
            EnsureDirectionalLights();
            // the toggle only flips Disabled, so the rest of the (inspector-editable) shadow
            // settings survive turning shadows off and on
            entityManager.GetComponent<CascadeShadowMap>(shadowConfigEntity).Disabled = gameProperties.DisableShadows;
            ref var sunLight = ref entityManager.GetComponent<Light>(sunEntity);
            ref var moonLight = ref entityManager.GetComponent<Light>(moonEntity);

            if (gameProperties.OverrideLighting)
            {
                sunLight.Color = Vector4.One;
                sunLight.AmbientColor = new Vector4(1, 1, 1, 0.4f);

                moonLight.Color = Vector4.One;
            }
            else if (BestLight != null)
            {
                var lightColor = BestLight.NormalWeather.GetLightParameter(LightIntParamType.GeneralLightning).GetColorAtTime(timeManager.Time);
                var ambientLight = BestLight.NormalWeather.GetLightParameter(LightIntParamType.AmbientLight).GetColorAtTime(timeManager.Time);

                sunLight.Color = lightColor.ToRgbaVector();
                sunLight.AmbientColor = ambientLight.ToRgbaVector();

                moonLight.Color = lightColor.ToRgbaVector();

                // fog is now a per-camera property; drive the game view camera's fog from the sky.
                var fogEnd = Math.Max(BestLight.NormalWeather.GetLightParameter(LightFloatParamType.FogDistance).GetAtTime(timeManager.Time) / 36, 1000);
                var mult = Math.Max(BestLight.NormalWeather.GetLightParameter(LightFloatParamType.FogMultiplier).GetAtTime(timeManager.Time), 0.5f);
                gameContext.Engine.CameraManager.MainCamera.Fog = new FogSettings()
                {
                    Enabled = true,
                    Color = BestLight.NormalWeather.GetLightParameter(LightIntParamType.FogInTheBackground).GetColorAtTime(timeManager.Time).ToRgbaVector(),
                    End = fogEnd,
                    Start = fogEnd * mult,
                };
            }

            float timeOfDay = (timeManager.Time.TotalMinutes + timeManager.MinuteFraction) / 1440.0f;
            float angle = (timeOfDay - 8 / 24.0f) * (float)Math.PI * 2;
            var sin = (float)Math.Sin(angle);
            var cos = (float)Math.Cos(angle);
            Vector3 sunPosition = new Vector3(0, -cos, sin);//sin * 30);//sin * 20, cos * 10, cos * 20);
            var sunForward = Vectors.Normalize(Vector3.Zero - sunPosition);
            var moonForward = -sunForward;

            // keep type/shadow flags pinned, set per-frame intensity, and bake the direction into the
            // entity transform (the engine derives light direction from LocalToWorld forward).
            sunLight.Type = LightType.Directional;
            sunLight.CastShadows = true;
            sunLight.Intensity = Math.Max(Vector3.Dot(Vectors.Down, sunForward), 0);
            moonLight.Type = LightType.Directional;
            moonLight.CastShadows = false;
            moonLight.Intensity = 0.5f * Math.Max(Vector3.Dot(Vectors.Down, moonForward), 0);

            entityManager.GetComponent<LocalToWorld>(sunEntity).Matrix = Matrix.CreateFromQuaternion(Utilities.LookRotation(sunForward, Vectors.Up));
            entityManager.GetComponent<LocalToWorld>(moonEntity).Matrix = Matrix.CreateFromQuaternion(Utilities.LookRotation(moonForward, Vectors.Up));

            gameContext.Archetypes.M2PointLightsArchetype.ParallelForEachState<LightingManager, Light, AnimatedM2Light>(this, static (that, itr, thread, start, end,
                lights, m2Lights) =>
            {
                for (int i = start; i < end; ++i)
                {
                    var length = m2Lights[i].M2.sequences[0].duration;
                    ref var light = ref lights[i];
                    ref var time = ref m2Lights[i]._time;
                    light.Type = LightType.Point;
                    light.AttenuationStart = AnimationSystem.Get(0, ref m2Lights[i].M2Light.attenuation_start, 0, time, Lerp);
                    light.AttenuationEnd = AnimationSystem.Get(0, ref m2Lights[i].M2Light.attenuation_end, 0, time, Lerp) * 2 /* default barely affects anything, so *2 */;
                    light.Intensity = AnimationSystem.Get(0, ref m2Lights[i].M2Light.diffuse_intensity, 0, time, Lerp) * 2 /* default barely affects anything, so *2 */;
                    light.Color = new Vector4(AnimationSystem.Get<Vector3>(0, ref m2Lights[i].M2Light.diffuse_color, Vector3.Zero, time, Vector3.Lerp), 1);

                    time += that.cachedDelta;
                    if (time >= length)
                    {
                        time -= length;
                    }
                }
            });
        }

        private bool directionalLightsCreated;

        // Creates the sun/moon directional Light entities on first use (the direction is later baked
        // into each entity's LocalToWorld forward in Update()).
        private void EnsureDirectionalLights()
        {
            if (directionalLightsCreated)
                return;
            var directionalArchetype = entityManager.NewArchetype()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<Light>();
            sunEntity = entityManager.CreateEntity(directionalArchetype, "Sun");
            moonEntity = entityManager.CreateEntity(directionalArchetype, "Moon");
            entityManager.GetComponent<Light>(sunEntity) = new Light { Type = LightType.Directional, CastShadows = true };
            entityManager.GetComponent<Light>(moonEntity) = new Light { Type = LightType.Directional, CastShadows = false };

            // shadows render only while an enabled CascadeShadowMap entity exists (see CascadeShadowMap).
            shadowConfigEntity = entityManager.CreateEntity(entityManager.NewArchetype()
                .WithComponentData<CascadeShadowMap>(), "Shadow Config");
            entityManager.GetComponent<CascadeShadowMap>(shadowConfigEntity) = CascadeShadowMap.CreateDefault();

            directionalLightsCreated = true;
        }

        private static float Lerp(float value1, float value2, float amount)
        {
            return (value1 * (1.0f - amount)) + (value2 * amount);
        }

        public void Render()
        {
            Time time = timeManager.Time;

            if (BestLight != null)
            {
                //-30, 225
                float preciseMinutes = (timeManager.Time.TotalMinutes + timeManager.MinuteFraction);
                float timeOfDay = preciseMinutes / 1440.0f;
                float timeOfDayHalf = ((preciseMinutes + 1440/2.0f) % 1440) / 1440.0f;
                
                var top = BestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyTopMost).GetColorAtTime(time);
                var middle = BestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyMiddle).GetColorAtTime(time);
                var towardsHorizon = BestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyToHorizon).GetColorAtTime(time);
                var horizon = BestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyHorizon).GetColorAtTime(time);
                var justAboveHorizon = BestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyJustAboveHorizon).GetColorAtTime(time);
                var sunColor = BestLight.NormalWeather.GetLightParameter(LightIntParamType.SunColor).GetColorAtTime(time);
                var cloudsColor1 = BestLight.NormalWeather.GetLightParameter(LightIntParamType.Clouds1).GetColorAtTime(time);
                var cloudsDensity = BestLight.NormalWeather.GetLightParameter(LightFloatParamType.CloudDensity).GetAtTime(time);
                material_data_t data = new material_data_t()
                {
                    top = top.ToRgbaVector(),
                    middle = middle.ToRgbaVector(),
                    horizon = horizon.ToRgbaVector(),
                    towardsHorizon = towardsHorizon.ToRgbaVector(),
                    justAboveHorizon = justAboveHorizon.ToRgbaVector(),
                    sunColor = sunColor.ToRgbaVector(),
                    cloudsColor1 = cloudsColor1.ToRgbaVector(),
                    timeOfDay = timeOfDay,
                    timeOfDayHalf = timeOfDayHalf,
                    cloudsDensity = cloudsDensity,
                    cloudsTexIndex = textureManager.GetBindlessIndex(noiseTexture),
                };
                skyMaterial.SetMaterialData(ref data);

                var localToWorld = new LocalToWorld()
                {
                    Matrix = Utilities.TRS(Vector3.Zero, Quaternion.Identity,
                        Vector3.One * gameContext.Engine.CameraManager.MainCamera.FarClip)
                };
                renderManager.Render(skySphereMesh, skyMaterial, ShaderPassType.Forward, 0, localToWorld.Matrix, localToWorld.Inverse);
            }
        }
    }
}