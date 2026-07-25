using TheEngine.Entities;
using TheEngine.Interfaces;

namespace TheEngine.Data;

public struct SceneData
{
    public SceneData(ICamera sceneCamera,
        DirectionalLightData mainLight,
        DirectionalLightData secondaryLight)
    {
        SceneCamera = sceneCamera;
        MainLight = mainLight;
        SecondaryLight = secondaryLight;
    }

    public ICamera SceneCamera { get; }
    public DirectionalLightData MainLight { get; }
    public DirectionalLightData SecondaryLight { get; }

    // Fog now lives on the camera (SceneCamera.Fog); resolved in RenderManager.UpdateSceneBuffer.
}
