using TheEngine.ECS;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Entities
{
    public class Camera : ICamera, IManagedComponentData
    {
        public Transform Transform { get; }

        public float FOV { get; set; }
        public float NearClip { get; set; }
        public float FarClip { get; set; }
        public float Aspect { get; set; }
        public FogSettings Fog { get; set; }
        public Color4 BackgroundColor { get; set; }

        private float viewDistanceModifier = 8;
        public float ViewDistanceModifier
        {
            get => viewDistanceModifier;
            set
            {
                if (value > 0)
                    viewDistanceModifier = value;
            }
        }

        public Matrix ProjectionMatrix => Matrix.CreatePerspectiveFieldOfView(MathUtil.DegreesToRadians(FOV), Aspect, NearClip, FarClip);
        public Matrix ViewMatrix => Transform.WorldToLocalMatrix;
        public Matrix InverseViewMatrix => Transform.LocalToWorldMatrix;

        public Camera()
        {
            Transform = new Transform();
            FOV = 60;
            NearClip = 1f;
            FarClip = 3660f;
            Fog = new FogSettings() { Enabled = false };
            BackgroundColor = new Color4(15 / 255f, 52 / 255f, 97 / 255f, 1);
        }
    }
}
