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

        internal LightManager(Engine engine)
        {
            this.engine = engine;
            MainLight = new DirectionalLight();
            MainLight.LightRotation = Quaternion.FromEuler(90, 45, 115);
            MainLight.LightColor = new TheMaths.Vector4(1, 1, 1, 1);
            MainLight.AmbientColor = new TheMaths.Vector4(0.2f, 0.2f, 0.2f, 1);
            MainLight.LightPosition = TheMaths.Vector3.Zero;
        }

        public void Dispose()
        {
            
        }
    }
}
