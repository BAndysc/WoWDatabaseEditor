using System.Runtime.InteropServices;
using TheMaths;

namespace TheEngine.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UniversalVertex
    {
        internal Vector4 position;
        internal Vector4 color;
        internal Vector4 normal;
        internal Vector2 uv1;
        internal Vector2 uv2;

        internal UniversalVertex(UniversalVertex other)
        {
            position = other.position;
            color = other.color;
            normal = other.normal;
            uv1 = other.uv1;
            uv2 = other.uv2;
        }
    }
}
