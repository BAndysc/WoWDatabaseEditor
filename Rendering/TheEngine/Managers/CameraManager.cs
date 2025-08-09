using System;
using TheEngine.Entities;
using TheEngine.Interfaces;

namespace TheEngine.Managers
{
    public class CameraManager : ICameraManager, IDisposable
    {
        private readonly Engine engine;

        public ICamera MainCamera { get; }
        public ICamera SceneViewCamera { get; }

        internal CameraManager(Engine engine)
        {
            this.engine = engine;
            var cameraArchetype = engine.entityManager.NewArchetype()
                .WithManagedComponentData<Camera>();
            var mainCameraEntity = engine.entityManager.CreateEntity(cameraArchetype, "Main camera");
            var sceneViewCameraEntity = engine.entityManager.CreateEntity(cameraArchetype, "Scene view camera");

            var mainCamera = new Camera();
            var sceneViewCamera = new Camera();

            this.engine.entityManager.SetManagedComponent(mainCameraEntity, mainCamera);
            this.engine.entityManager.SetManagedComponent(sceneViewCameraEntity, sceneViewCamera);

            MainCamera = mainCamera;
            SceneViewCamera = sceneViewCamera;
        }
        
        public void Dispose()
        {
            
        }
    }
}
