using System.Runtime.InteropServices;
using TheMaths;

namespace TheEngine.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UniversalVertex
    {
        internal Vector3 position;
        internal Vector3 normal;
        internal Vector2 uv1;
        internal Vector2 uv2;
        internal uint color;
        internal uint color2;

        internal UniversalVertex(UniversalVertex other)
        {
            position = other.position;
            color = other.color;
            color2 = other.color2;
            normal = other.normal;
            uv1 = other.uv1;
            uv2 = other.uv2;
        }
    }
}
