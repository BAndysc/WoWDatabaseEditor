using TheMaths;

namespace TheEngine.Entities
{
    public class DirectionalLight
    {
        public Vector4 LightColor { get; set; }
        public Quaternion LightRotation { get; set; }

        public Vector3 LightPosition { get; set; }
    }
}
