using TheEngine.Data;
using TheMaths;

namespace WDE.MapRenderer.StaticData
{
    public static class ChunkMesh
    {
        public static MeshData Create()
        {
            Vector3[] vertices = new Vector3[Constants.ChunksInBlock * Constants.VerticesInChunk];
            Vector2[] uvs = new Vector2[Constants.ChunksInBlock * Constants.VerticesInChunk];
            int[] indices = new int[Constants.ChunksInBlock * 4 * 8 * 8 * 3];
            int globalVertexOffset = 0;
            int globalIndicesOffset = 0;
            for (int i = 0; i < Constants.ChunksInBlockY; ++i)
            {
                for (int j = 0; j < Constants.ChunksInBlockX; ++j)
                {
                    int k = 0;
                    for (int cy = 0; cy < 17; ++cy)
                    {
                        for (int cx = 0; cx < (cy % 2 == 0 ? 9 : 8); cx++)
                        {
                            float VERTX = 0, VERTX2 = 0;
                            if (cy % 2 == 0)
                            {
                                VERTX = (8 - cx) / 8.0f * Constants.ChunkSize;
                                VERTX2 = (cx) / 8.0f * Constants.ChunkSize;
                            }
                            else
                            {
                                VERTX = (Constants.ChunkSize / 8) * 7 * ((7 - cx) / 7.0f) + Constants.ChunkSize / 8 / 2;
                                VERTX2 = (Constants.ChunkSize / 8) * 7 * ((cx) / 7.0f) + Constants.ChunkSize / 8 / 2;
                            }

                            float VERTY = (16 - cy) / 16.0f * Constants.ChunkSize;
                            float VERTY2 = (cy) / 16.0f * Constants.ChunkSize;
                            var vert = new Vector3( VERTY + (Constants.ChunksInBlockX - i) * Constants.ChunkSize - 533.333333f - Constants.ChunkSize, VERTX + (Constants.ChunksInBlockY - j) * Constants.ChunkSize - 533.333333f - Constants.ChunkSize, 0);
                            vertices[k + globalVertexOffset] = vert;
                            uvs[k + globalVertexOffset] = new Vector2(VERTX2 / Constants.ChunkSize, VERTY2 / Constants.ChunkSize);
                            k++;
                        }
                    }

                    for (int cx = 0; cx < 8; cx++)
                    {
                        for (int cy = 0; cy < 8; cy++)
                        {
                            int tl = globalVertexOffset + cy * 17 + cx;
                            int tr = tl + 1;
                            int middle = tl + 9;
                            int bl = middle + 8;
                            int br = bl + 1;

                            indices[globalIndicesOffset++] = tl;
                            indices[globalIndicesOffset++] = middle;
                            indices[globalIndicesOffset++] = tr;
                            //
                            indices[globalIndicesOffset++] = tl;
                            indices[globalIndicesOffset++] = bl;
                            indices[globalIndicesOffset++] = middle;
                            //
                            indices[globalIndicesOffset++] = tr;
                            indices[globalIndicesOffset++] = middle;
                            indices[globalIndicesOffset++] = br;
                            //
                            indices[globalIndicesOffset++] = middle;
                            indices[globalIndicesOffset++] = bl;
                            indices[globalIndicesOffset++] = br;
                        }
                    }

                    globalVertexOffset += Constants.VerticesInChunk;
                }
            }

            return new MeshData(vertices, new Vector3[vertices.Length], uvs, indices);
        }
    }
}