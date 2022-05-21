using TheEngine.Data;
using TheMaths;

namespace TheEngine.Primitives
{
    public class SkyboxMesh
    {
        private static MeshData meshData;
        public static ref MeshData Instance => ref meshData;

        static SkyboxMesh()
        {
            meshData = new MeshData(VERTICES, null, null, indices);
        }
        
        private static float SIZE = 1f;

        private static Vector3[] VERTICES = {
            new Vector3(-SIZE,  -SIZE, -SIZE),
            new Vector3(-SIZE, -SIZE, SIZE),
            new Vector3(SIZE, -SIZE, SIZE),
            new Vector3(SIZE, -SIZE, -SIZE),

            new Vector3(-SIZE,  SIZE, -SIZE),
            new Vector3(-SIZE, SIZE, SIZE),
            new Vector3(SIZE, SIZE, SIZE),
            new Vector3(SIZE, SIZE, -SIZE),
        };
        
        private static ushort[] indices =
        {
            0,3,1,
            1,3,2,

            0,1,4,
            1,5,4,

            7,2,3,
            7,6,2,

            1,2,5,
            5,2,6,

            0,4,3,
            3,4,7,

            4,5,7,
            5,6,7,
        };
    }
}
