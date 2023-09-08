using TheEngine.Entities;
using TheEngine.Interfaces;

namespace TheEngine.Data;

public struct SceneData
{
    public SceneData(ICamera sceneCamera,
        FogSettings fog, 
        DirectionalLight mainLight,
        DirectionalLight secondaryLight)
    {
        SceneCamera = sceneCamera;
        Fog = fog;
        MainLight = mainLight;
        SecondaryLight = secondaryLight;
    }

    public ICamera SceneCamera { get; }
    public FogSettings Fog { get; }
    public DirectionalLight MainLight { get; }
    public DirectionalLight SecondaryLight { get; }
}