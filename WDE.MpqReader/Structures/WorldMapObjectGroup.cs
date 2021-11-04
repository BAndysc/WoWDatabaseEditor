using System.Diagnostics;
using TheMaths;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public class WorldMapObjectGroup : System.IDisposable
    {
        public WorldMapObjectGroupHeader Header { get; init; }
        public PooledArray<int> Indices { get; init; }
        public PooledArray<Vector3> Vertices { get; init; }
        public PooledArray<Vector3> Normals { get; init; }
        public PooledArray<Vector2> UVs { get; init; }
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

                if (chunkName == "MOVI")
                    Indices = ParseIndices(partialReader, size);
                else if (chunkName == "MOVT")
                    Vertices = ReadVectors3(partialReader, openGlCoords, size);
                else if (chunkName == "MONR")
                    Normals = ReadVectors3(partialReader, openGlCoords, size);
                else if (chunkName == "MOTV")
                    UVs = ReadVectors2(partialReader, size);
                else if (chunkName == "MOBA")
                    Batches = ReadBatches(partialReader, size);
            
                reader.Offset = offset + size;
            }
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

        private static PooledArray<Vector3> ReadVectors3(IBinaryReader reader, bool openGlCoords, int size)
        {
            PooledArray<Vector3> vertices = new PooledArray<Vector3>(size / 12); // each vector has 12 bytes
            int i = 0;
            while (!reader.IsFinished())
            {
                vertices[i++] = openGlCoords ? reader.ReadOpenGlVector3() : reader.ReadVector3();
            }

            return vertices;
        }

        private static PooledArray<int> ParseIndices(IBinaryReader reader, int size)
        {
            PooledArray<int> indices = new PooledArray<int>(size / 2);
            int i = 0;
            while (!reader.IsFinished())
            {
                indices[i++] = reader.ReadUInt16();
            }

            return indices;
        }

        public void Dispose()
        {
            Indices.Dispose();
            Vertices.Dispose();
            Normals.Dispose();
            UVs.Dispose();
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