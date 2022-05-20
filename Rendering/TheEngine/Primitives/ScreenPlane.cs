using TheEngine.Data;
using TheMaths;

namespace TheEngine.Primitives
{
    public class ScreenPlane
    {
        private static MeshData meshData;
        public static ref MeshData Instance => ref meshData;

        static ScreenPlane()
        {
            meshData = new MeshData(VERTICES, null, UVs, indices);
        }

        private static Vector3[] VERTICES = {
            new Vector3(-1, 1, 0),
            new Vector3(-1, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(1, 1, 0),
        };
        
        private static Vector2[] UVs = {
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
        };


        private static ushort[] indices =
        {
            0, 1, 2,
            0, 2, 3
        };
    }
}
