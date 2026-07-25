using TheEngine.Data;
using TheMaths;

namespace TheEngine.Primitives
{
    public class CubeMesh
    {
        private static MeshData meshData;
        public static ref MeshData Instance => ref meshData;

        static CubeMesh()
        {
            meshData = new MeshData(VERTICES, NORMALS, null, indices);
        }

        private static float SIZE = 0.5f;

        // 4 duplicated vertices per face (24 total) rather than the 8 shared corners, so each face
        // can have its own flat outward normal - PBR lighting needs a real per-vertex normal attribute,
        // not one averaged across the 3 faces sharing a corner.
        private static Vector3[] VERTICES =
        {
            // -Z
            new Vector3(-SIZE, -SIZE, -SIZE),
            new Vector3(SIZE, -SIZE, -SIZE),
            new Vector3(SIZE, SIZE, -SIZE),
            new Vector3(-SIZE, SIZE, -SIZE),
            // +Z
            new Vector3(-SIZE, -SIZE, SIZE),
            new Vector3(SIZE, -SIZE, SIZE),
            new Vector3(SIZE, SIZE, SIZE),
            new Vector3(-SIZE, SIZE, SIZE),
            // -X
            new Vector3(-SIZE, -SIZE, -SIZE),
            new Vector3(-SIZE, -SIZE, SIZE),
            new Vector3(-SIZE, SIZE, SIZE),
            new Vector3(-SIZE, SIZE, -SIZE),
            // +X
            new Vector3(SIZE, -SIZE, -SIZE),
            new Vector3(SIZE, -SIZE, SIZE),
            new Vector3(SIZE, SIZE, SIZE),
            new Vector3(SIZE, SIZE, -SIZE),
            // -Y
            new Vector3(-SIZE, -SIZE, -SIZE),
            new Vector3(SIZE, -SIZE, -SIZE),
            new Vector3(SIZE, -SIZE, SIZE),
            new Vector3(-SIZE, -SIZE, SIZE),
            // +Y
            new Vector3(-SIZE, SIZE, -SIZE),
            new Vector3(SIZE, SIZE, -SIZE),
            new Vector3(SIZE, SIZE, SIZE),
            new Vector3(-SIZE, SIZE, SIZE),
        };

        private static Vector3[] NORMALS =
        {
            new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1), new Vector3(0, 0, -1),
            new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 1),
            new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0),
            new Vector3(0, -1, 0), new Vector3(0, -1, 0), new Vector3(0, -1, 0), new Vector3(0, -1, 0),
            new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0),
        };

        private static ushort[] indices =
        {
            0, 2, 1, 0, 3, 2,
            4, 6, 5, 4, 7, 6,
            8, 10, 9, 8, 11, 10,
            12, 14, 13, 12, 15, 14,
            16, 18, 17, 16, 19, 18,
            20, 22, 21, 20, 23, 22,
        };
    }
}
