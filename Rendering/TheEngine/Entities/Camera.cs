using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Entities
{
    public class Camera : ICamera
    {
        public Transform Transform { get; }

        public float FOV { get; set; }
        public float NearClip { get; set; }
        public float FarClip { get; set; }
        public float Aspect { get; set; }
        public Matrix ProjectionMatrix => Matrix.CreatePerspectiveFieldOfView(MathUtil.DegreesToRadians(FOV), Aspect, NearClip, FarClip);
        public Matrix ViewMatrix => Transform.WorldToLocalMatrix;
        public Matrix InverseViewMatrix => Transform.LocalToWorldMatrix;

        public Camera()
        {
            Transform = new Transform();
            FOV = 60;
            NearClip = 1f;
            FarClip = 36600f;
        }
    }
}
