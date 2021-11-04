using TheEngine.Entities;
using TheEngine.Interfaces;

namespace TheEngine.Managers
{
    public class CameraManager : ICameraManager, IDisposable
    {
        private readonly Engine engine;

        public ICamera MainCamera { get; }

        internal CameraManager(Engine engine)
        {
            this.engine = engine;
            MainCamera = new Camera();
        }
        
        public void Dispose()
        {
            
        }
    }
}
