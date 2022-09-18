using TheEngine.Entities;
using TheEngine.Interfaces;

namespace TheEngine.Data;

public struct SceneData
{
    public SceneData(ICamera sceneCamera, DirectionalLight mainLight, DirectionalLight secondaryLight)
    {
        SceneCamera = sceneCamera;
        MainLight = mainLight;
        SecondaryLight = secondaryLight;
    }

    public ICamera SceneCamera { get; }
    public DirectionalLight MainLight { get; }
    public DirectionalLight SecondaryLight { get; }
}