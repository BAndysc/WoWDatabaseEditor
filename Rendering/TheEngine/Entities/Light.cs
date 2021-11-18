using TheMaths;

namespace TheEngine.Entities
{
    public class DirectionalLight
    {
        public Vector4 AmbientColor { get; set; }
        public Vector4 LightColor { get; set; }
        public Quaternion LightRotation { get; set; }
        public Vector3 LightPosition { get; set; }
        public float LightIntensity { get; set; }
    }
}
