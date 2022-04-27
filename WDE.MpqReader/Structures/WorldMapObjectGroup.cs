using System;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using TheMaths;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public class WorldMapObjectGroup : System.IDisposable
    {
        public WorldMapObjectGroupHeader Header { get; init; }
        public PooledArray<WorldMapObjectPoly> Polygons { get; set; }
        public PooledArray<int> Indices { get; init; }
        public PooledArray<Vector3> Vertices { get; init; }
        public PooledArray<Vector4>? VertexColors { get; init; }
        public int[] CollisionOnlyIndices { get; init; }
        public PooledArray<Vector3> Normals { get; init; }
        public List<PooledArray<Vector2>> UVs { get; init; } = new();
        public WorldMapObjectBatch[] Batches { get; init; }
    
        public WorldMapObjectGroup(IBinaryReader reader, bool openGlCoords)
        {
            var firstChunkName = reader.ReadBytes(4);
            Debug.Assert(firstChunkName[0] == 'R' && firstChunkName[1] == 'E' && firstChunkName[2] == 'V' && firstChunkName[3] == 'M');
            reader.ReadInt32();
            reader.ReadInt32(); // version

            var secondChunkName = reader.ReadBytes(4);
            Debug.Assert(secondChunkName[0] == 'P' && secondChunkName[1] == 'G' && secondChunkName[2] == 'O' && secondChunkName[3] == 'M');
            reader.ReadInt32();
            Header = new WorldMapObjectGroupHeader(reader);
        
            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                var partialReader = new LimitedReader(reader, size);

                if (chunkName == "MOPY")
                    Polygons = ParsePolygons(partialReader, size);
                else if (chunkName == "MOVI")
                    Indices = ParseIndices(partialReader, size, Polygons);
                else if (chunkName == "MOVT")
                    Vertices = ReadVectors3(partialReader, size);
                else if (chunkName == "MONR")
                    Normals = ReadVectors3(partialReader, size);
                else if (chunkName == "MOTV")
                    UVs.Add(ReadVectors2(partialReader, size));
                else if (chunkName == "MOBA")
                    Batches = ReadBatches(partialReader, size);
                else if (chunkName == "MOCV")
                    VertexColors = ReadVertexColors(partialReader, size);

                CollisionOnlyIndices = BuildCollisionOnlyIndices(Polygons, Indices);
                
                reader.Offset = offset + size;
            }
        }

        private static int[] BuildCollisionOnlyIndices(PooledArray<WorldMapObjectPoly>? polygons, PooledArray<int>? indices)
        {
            if (polygons == null || indices == null)
                return new int[] { };
            int triangles = 0;
            for (int i = 0; i < polygons.Length; ++i)
            {
                if (polygons[i].IsCollisionOnly)
                    triangles++;
            }

            var collisionOnlyIndices = new int[triangles * 3];
            int j = 0;
            for (int i = 0; i < polygons.Length; ++i)
            {
                if (polygons[i].IsCollisionOnly)
                {
                    collisionOnlyIndices[j++] = indices[i * 3];
                    collisionOnlyIndices[j++] = indices[i * 3 + 1];
                    collisionOnlyIndices[j++] = indices[i * 3 + 2];
                }
            }

            return collisionOnlyIndices;
        }

        private PooledArray<Vector4> ReadVertexColors(IBinaryReader reader, int size)
        {
            PooledArray<Vector4> colors = new PooledArray<Vector4>(size / 4);
            int i = 0;
            while (!reader.IsFinished())
            {
                var b = reader.ReadByte() / 255.0f;
                var g = reader.ReadByte() / 255.0f;
                var r = reader.ReadByte() / 255.0f;
                var a = reader.ReadByte() / 255.0f;
                colors[i++] = new Vector4(r, g, b, a);
            }

            return colors;
        }
        
        private static PooledArray<WorldMapObjectPoly> ParsePolygons(IBinaryReader reader, int size)
        {
            PooledArray<WorldMapObjectPoly> polygons = new PooledArray<WorldMapObjectPoly>(size / 2);
            int i = 0;
            while (!reader.IsFinished())
                polygons[i++] = new WorldMapObjectPoly(reader);

            return polygons;
        }

        private static WorldMapObjectBatch[] ReadBatches(IBinaryReader reader, int size)
        {
            WorldMapObjectBatch[] batches = new WorldMapObjectBatch[size / 24];
            int i = 0;
            while (!reader.IsFinished())
            {
                batches[i++] = new WorldMapObjectBatch(reader);
            }

            return batches;
        }

        private static PooledArray<Vector2> ReadVectors2(IBinaryReader reader, int size)
        {
            PooledArray<Vector2> vertices = new PooledArray<Vector2>(size / 8); // each vector has 8 bytes
            int i = 0;
            while (!reader.IsFinished())
            {
                vertices[i++] = reader.ReadVector2();
            }

            return vertices;
        }

        private static PooledArray<Vector3> ReadVectors3(IBinaryReader reader, int size)
        {
            PooledArray<Vector3> vertices = new PooledArray<Vector3>(size / 12); // each vector has 12 bytes
            int i = 0;
            while (!reader.IsFinished())
            {
                vertices[i++] = reader.ReadVector3();
            }

            return vertices;
        }

        private static PooledArray<int> ParseIndices(IBinaryReader reader, int size, PooledArray<WorldMapObjectPoly> polygons)
        {
            PooledArray<int> indices = new PooledArray<int>(size / 2);
            int i = 0;
            while (!reader.IsFinished())
            {
                indices[i++] = reader.ReadUInt16();
                indices[i++] = reader.ReadUInt16();
                indices[i++] = reader.ReadUInt16();
            }

            return indices;
        }

        public void Dispose()
        {
            VertexColors?.Dispose();
            Polygons.Dispose();
            Indices.Dispose();
            Vertices.Dispose();
            Normals.Dispose();
            foreach (var uv in UVs)
                uv.Dispose();
        }
    }

    public struct WorldMapObjectPoly
    {
        [Flags]
        public enum Flags
        {
            /*0x01*/ F_UNK_0x01 = 0x1,
            /*0x02*/ F_NOCAMCOLLIDE = 0x2,
            /*0x04*/ F_DETAIL = 0x4,
            /*0x08*/ F_COLLISION = 0x8, // Turns off rendering of water ripple effects. May also do more. Should be used for ghost material triangles.
            /*0x10*/ F_HINT = 0x10,
            /*0x20*/ F_RENDER = 0x20,
            /*0x40*/ F_UNK_0x40 = 0x40,
            /*0x80*/ F_COLLIDE_HIT = 0x80,
        }

        public Flags flags;
        public byte materialId;

        public bool IsCollisionOnly => materialId == 0xFF;
        
        public WorldMapObjectPoly(IBinaryReader reader)
        {
            flags = (Flags)reader.ReadByte();
            materialId = reader.ReadByte();
        }
    }

    public struct WorldMapObjectBatch
    {
        public ushort bx, by, bz; // a bounding box for culling, see "unknown_box" below
        public short tx, ty, tz;
        public uint startIndex;                     // index of the first face index used in MOVI
        public ushort count; // number of MOVI indices used
        public ushort minIndex; // index of the first vertex used in MOVT
        public ushort maxIndex; // index of the last vertex used (batch includes this one)
        public byte flag_unknown_1;
        public byte material_id; // index in MOMT

        public WorldMapObjectBatch(IBinaryReader reader)
        {
            bx = reader.ReadUInt16();
            by = reader.ReadUInt16();
            bz = reader.ReadUInt16();
            tx = reader.ReadInt16();
            ty = reader.ReadInt16();
            tz = reader.ReadInt16();
            startIndex = reader.ReadUInt32();
            count = reader.ReadUInt16();
            minIndex = reader.ReadUInt16();
            maxIndex = reader.ReadUInt16();
            flag_unknown_1 = reader.ReadByte();
            material_id = reader.ReadByte();
        }
    }

    public struct WorldMapObjectGroupHeader
    {
        public uint groupName;               // offset into MOGN
        public uint descriptiveGroupName;    // offset into MOGN
        public uint flags;                   // see below
        public CAaBox boundingBox;              // as with flags, same as in corresponding MOGI entry

        public ushort portalStart;             // index into MOPR
        public ushort portalCount;             // number of MOPR items used after portalStart

        public ushort transBatchCount;
        public ushort intBatchCount;
        public ushort extBatchCount;
        public ushort padding_or_batch_type_d; // probably padding, but might be data?

        public byte[] fogIds;                // ids in MFOG
        public uint groupLiquid;             // see below in the MLIQ chunk

        public uint uniqueID;

        public uint flags2;
        public uint unk;                     // UNUSED: 20740

        public WorldMapObjectGroupHeader(IBinaryReader reader)
        {
            groupName = reader.ReadUInt32();
            descriptiveGroupName = reader.ReadUInt32();
            flags = reader.ReadUInt32();
            boundingBox = CAaBox.Read(reader);
            portalStart = reader.ReadUInt16();
            portalCount = reader.ReadUInt16();
            transBatchCount = reader.ReadUInt16();
            intBatchCount = reader.ReadUInt16();
            extBatchCount = reader.ReadUInt16();
            padding_or_batch_type_d = reader.ReadUInt16();
            fogIds = new byte[] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
            groupLiquid = reader.ReadUInt32();
            uniqueID = reader.ReadUInt32();
            flags2 = reader.ReadUInt32();
            unk = reader.ReadUInt32();
        }
    }
}