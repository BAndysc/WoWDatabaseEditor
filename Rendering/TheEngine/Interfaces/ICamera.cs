using TheEngine.Entities;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface ICamera
    {
        Transform Transform { get; }
        float FOV { get; set; }

        float NearClip { get; set; }
        float FarClip { get; set; }
        float Aspect { get; set; }
        Matrix ProjectionMatrix { get; }
        Matrix ViewMatrix { get; }
        Matrix InverseViewMatrix { get; }
    }
}
