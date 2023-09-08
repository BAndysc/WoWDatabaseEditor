using System;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
    public class LightManager : ILightManager, IDisposable
    {
        private readonly Engine engine;

        public DirectionalLight MainLight { get; set; }
        public DirectionalLight SecondaryLight { get; set; }
        private FogSettings fog;
        public ref FogSettings Fog => ref fog;
        
        internal LightManager(Engine engine)
        {
            this.engine = engine;
            MainLight = new DirectionalLight();
            MainLight.LightRotation = Utilities.FromEuler(90, 45, 115);
            MainLight.LightColor = new Vector4(1, 1, 1, 1);
            MainLight.AmbientColor = new Vector4(0.2f, 0.2f, 0.2f, 1);
            MainLight.LightPosition = Vector3.Zero;
            
            SecondaryLight = new DirectionalLight();
            SecondaryLight.LightIntensity = 0;
            SecondaryLight.AmbientColor = Vector4.Zero;

            Fog = new FogSettings()
            {
                Enabled = false,
                Start = 0,
                End = 0,
                Color = new Vector4(0, 0, 0, 1)
            };
        }

        public void Dispose()
        {
            
        }
    }
}
