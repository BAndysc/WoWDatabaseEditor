using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer.StaticData;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
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
        private IMesh skySphereMesh;
        private Material skyMaterial;
        private TextureHandle noiseTexture;

        public LightingManager(IGameContext gameContext,
            IGameProperties gameProperties,
            IMeshManager meshManager, 
            IMaterialManager materialManager,
            ITextureManager textureManager,
            IRenderManager renderManager,
            CameraManager cameraManager,
            LightStore lightStore,
            ILightManager lightManager,
            TimeManager timeManager)
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
            skySphereMesh = meshManager.CreateMesh(ObjParser.LoadObj("meshes/skysphere.obj").MeshData);
            skyMaterial = materialManager.CreateMaterial("data/skybox.json");
            noiseTexture = textureManager.LoadTexture("textures/noise_512.png");
            
            skyMaterial.SetTexture("cloudsTex", noiseTexture);
        }

        public void Dispose()
        {
            meshManager.DisposeMesh(skySphereMesh);
            textureManager.DisposeTexture(noiseTexture);
        }

        private Light? bestLight = null;
        public void Update(float delta)
        {
            var position = cameraManager.Position;
            var bestDistance = float.MaxValue;
            foreach (var lightning in lightStore)
            {
                if (lightning.Continent != gameContext.CurrentMap.Id)
                    continue;
                var pos = new Vector3(lightning.X, lightning.Y, lightning.Z);
                var distance = (pos - position).Length();
                if (distance < lightning.FalloffEnd)
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestLight = lightning;
                    }
                }
            }
            
            if (gameProperties.OverrideLighting)
            {
                lightManager.MainLight.LightColor = Vector4.One;
                lightManager.MainLight.LightIntensity = 0.8f;
                lightManager.MainLight.AmbientColor = new Vector4(1, 1, 1, 0.4f);
                
                lightManager.SecondaryLight.LightColor = Vector4.One;
                lightManager.SecondaryLight.LightIntensity = 0;
            }
            else if (bestLight != null)
            {
                var lightColor = bestLight.NormalWeather.GetLightParameter(LightIntParamType.GeneralLightning).GetColorAtTime(timeManager.Time);
                var ambientLight = bestLight.NormalWeather.GetLightParameter(LightIntParamType.AmbientLight).GetColorAtTime(timeManager.Time);

                lightManager.MainLight.LightColor = lightColor.ToRgbaVector();
                lightManager.MainLight.AmbientColor = ambientLight.ToRgbaVector();
                
                lightManager.SecondaryLight.LightColor = lightColor.ToRgbaVector();
            }

            float timeOfDay = (timeManager.Time.TotalMinutes + timeManager.MinuteFraction) / 1440.0f;
            float angle = (timeOfDay - 8 / 24.0f) * (float)Math.PI * 2;
            var sin = (float)Math.Sin(angle);
            var cos = (float)Math.Cos(angle);
            Vector3 sunPosition = new Vector3(0, -cos, sin);//sin * 30);//sin * 20, cos * 10, cos * 20);
            var sunForward = (Vector3.Zero - sunPosition).Normalized;
            var moonForward = -sunForward;

            lightManager.MainLight.LightPosition = sunPosition;
            lightManager.MainLight.LightRotation = Quaternion.LookRotation(sunForward, Vector3.Up);
            lightManager.MainLight.LightIntensity = Math.Max(Vector3.Dot(Vector3.Down, sunForward), 0);
            
            lightManager.SecondaryLight.LightRotation = Quaternion.LookRotation(moonForward, Vector3.Up);
            lightManager.SecondaryLight.LightIntensity = 0.5f * Math.Max(Vector3.Dot(Vector3.Down, moonForward), 0);
        }

        public void Render()
        {
            Time time = timeManager.Time;

            if (bestLight != null)
            {
                //-30, 225
                float preciseMinutes = (timeManager.Time.TotalMinutes + timeManager.MinuteFraction);
                float timeOfDay = preciseMinutes / 1440.0f;
                float timeOfDayHalf = ((preciseMinutes + 1440/2.0f) % 1440) / 1440.0f;
                
                var top = bestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyTopMost).GetColorAtTime(time);
                var middle = bestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyMiddle).GetColorAtTime(time);
                var towardsHorizon = bestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyToHorizon).GetColorAtTime(time);
                var horizon = bestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyHorizon).GetColorAtTime(time);
                var justAboveHorizon = bestLight.NormalWeather.GetLightParameter(LightIntParamType.SkyJustAboveHorizon).GetColorAtTime(time);
                var sunColor = bestLight.NormalWeather.GetLightParameter(LightIntParamType.SunColor).GetColorAtTime(time);
                var cloudsColor1 = bestLight.NormalWeather.GetLightParameter(LightIntParamType.Clouds1).GetColorAtTime(time);
                var cloudsDensity = bestLight.NormalWeather.GetLightParameter(LightFloatParamType.CloudDensity).GetAtTime(time);
                skyMaterial.BlendingEnabled = true;
                skyMaterial.SourceBlending = Blending.SrcAlpha;
                skyMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
                skyMaterial.SetUniform("top", top.ToRgbaVector());
                skyMaterial.SetUniform("middle", middle.ToRgbaVector());
                skyMaterial.SetUniform("horizon", horizon.ToRgbaVector());
                skyMaterial.SetUniform("towardsHorizon", towardsHorizon.ToRgbaVector());
                skyMaterial.SetUniform("justAboveHorizon", justAboveHorizon.ToRgbaVector());
                skyMaterial.SetUniform("sunColor", sunColor.ToRgbaVector());
                skyMaterial.SetUniform("cloudsColor1", cloudsColor1.ToRgbaVector());
                skyMaterial.SetUniform("timeOfDay", timeOfDay);
                skyMaterial.SetUniform("timeOfDayHalf", timeOfDayHalf);
                skyMaterial.SetUniform("cloudsDensity", cloudsDensity);
                
                var t = new Transform();
                t.Scale = Vector3.One * 10000f;
                renderManager.Render(skySphereMesh, skyMaterial, 0, t);
            }
        }
    }
}