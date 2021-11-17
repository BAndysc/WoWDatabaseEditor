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
        private IMesh skySphereMesh;
        private Material skyMaterial;
        private TextureHandle noiseTexture;

        public LightingManager(IGameContext gameContext)
        {
            this.gameContext = gameContext;
            skySphereMesh = gameContext.Engine.MeshManager.CreateMesh(ObjParser.LoadObj("meshes/skysphere.obj").MeshData);
            skyMaterial = gameContext.Engine.MaterialManager.CreateMaterial("data/skybox.json");
            noiseTexture = gameContext.Engine.TextureManager.LoadTexture("textures/noise_512.png");
            
            skyMaterial.SetTexture("cloudsTex", noiseTexture);
        }

        public bool OverrideLighting { get; set; }

        public void Dispose()
        {
            gameContext.Engine.MeshManager.DisposeMesh(skySphereMesh);
            gameContext.Engine.TextureManager.DisposeTexture(noiseTexture);
        }

        private Light? bestLight = null;
        public void Update(float delta)
        {
            var position = gameContext.CameraManager.Position.ToWoWPosition();
            var bestDistance = float.MaxValue;
            foreach (var lightning in gameContext.DbcManager.LightStore)
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
            
            
            if (OverrideLighting)
            {
                gameContext.Engine.LightManager.MainLight.LightColor = Vector4.One;
                gameContext.Engine.LightManager.MainLight.LightIntensity = 0.8f;
                gameContext.Engine.LightManager.MainLight.AmbientColor = new Vector4(1, 1, 1, 0.4f);
                
                gameContext.Engine.LightManager.SecondaryLight.LightColor = Vector4.One;
                gameContext.Engine.LightManager.SecondaryLight.LightIntensity = 0;
            }
            else if (bestLight != null)
            {
                var lightColor = bestLight.NormalWeather.GetLightParameter(LightIntParamType.GeneralLightning).GetColorAtTime(gameContext.TimeManager.Time);
                var ambientLight = bestLight.NormalWeather.GetLightParameter(LightIntParamType.AmbientLight).GetColorAtTime(gameContext.TimeManager.Time);

                gameContext.Engine.LightManager.MainLight.LightColor = lightColor.ToRgbaVector();
                gameContext.Engine.LightManager.MainLight.AmbientColor = ambientLight.ToRgbaVector();
                
                gameContext.Engine.LightManager.SecondaryLight.LightColor = lightColor.ToRgbaVector();
            }

            float timeOfDay = (gameContext.TimeManager.Time.TotalMinutes + gameContext.TimeManager.MinuteFraction) / 1440.0f;
            float angle = (timeOfDay - 2 / 24.0f) * (float)Math.PI * 2 ; // offset by 2 hours
            var sin = (float)Math.Sin(angle);
            var cos = (float)Math.Cos(angle);
            Vector3 sunPosition = new Vector3(sin * 20, cos * 20, -cos * 10);
            var sunForward = (Vector3.Zero - sunPosition).Normalized;
            var moonForward = -sunForward;
            
            gameContext.Engine.LightManager.MainLight.LightPosition = sunPosition;
            gameContext.Engine.LightManager.MainLight.LightRotation = Quaternion.LookRotation(sunForward, Vector3.Up);
            gameContext.Engine.LightManager.MainLight.LightIntensity = Math.Max(Vector3.Dot(Vector3.Up, sunForward), 0);
            
            gameContext.Engine.LightManager.SecondaryLight.LightRotation = Quaternion.LookRotation(moonForward, Vector3.Up);
            gameContext.Engine.LightManager.SecondaryLight.LightIntensity = 0.5f * Math.Max(Vector3.Dot(Vector3.Up, moonForward), 0);
        }

        public void Render()
        {
            Time time = gameContext.TimeManager.Time;

            if (bestLight != null)
            {
                //-30, 225
                float preciseMinutes = (gameContext.TimeManager.Time.TotalMinutes + gameContext.TimeManager.MinuteFraction);
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
                gameContext.Engine.RenderManager.Render(skySphereMesh, skyMaterial, 0, t);
            }
            
            using var ui = gameContext.Engine.Ui.BeginImmediateDraw(0, 0);
            ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.7f), 5);
            ui.Text("calibri", $"{time.Hour:00}:{time.Minute:00}", 16, Vector4.One);
            ui.EndBox();
        }
    }
}