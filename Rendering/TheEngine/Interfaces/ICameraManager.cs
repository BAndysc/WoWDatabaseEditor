namespace TheEngine.Interfaces
{
    public interface ICameraManager
    {
        ICamera MainCamera { get; }
        ICamera SceneViewCamera { get; }
    }
}
