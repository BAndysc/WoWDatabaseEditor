using System;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;
using TheMaths;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public class WorldMapObjectGroup : System.IDisposable
    {
        public WorldMapObjectGroupHeader Header { get; init; }
        public PooledArray<WorldMapObjectPoly> Polygons { get; set; }
        public PooledArray<ushort> Indices { get; init; }
        public PooledArray<Vector3> Vertices { get; init; }
        public PooledArray<Color>? VertexColors { get; init; }
        public ushort[] CollisionOnlyIndices { get; init; }
        public PooledArray<Vector3> Normals { get; init; }
        public List<PooledArray<Vector2>> UVs { get; init; } = new();
        public WorldMapObjectBatch[] Batches { get; init; }
        public WorldMapObjectLiquid Liquid { get; init; }
    
        public WorldMapObjectGroup(IBinaryReader reader)
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
                else if (chunkName == "MLIQ")
                    Liquid = new WorldMapObjectLiquid(reader, Header);

                CollisionOnlyIndices = BuildCollisionOnlyIndices(Polygons, Indices);
                
                reader.Offset = offset + size;
            }
        }
        
        private static ushort[] BuildCollisionOnlyIndices(PooledArray<WorldMapObjectPoly>? polygons, PooledArray<ushort>? indices)
        {
            if (polygons == null || indices == null)
                return new ushort[] { };
            int triangles = 0;
            for (int i = 0; i < polygons.Length; ++i)
            {
                if (polygons[i].IsCollisionOnly)
                    triangles++;
            }

            var collisionOnlyIndices = new ushort[triangles * 3];
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

        private PooledArray<Color> ReadVertexColors(IBinaryReader reader, int size)
        {
            PooledArray<Color> colors = new PooledArray<Color>(size / 4);
            int i = 0;
            while (!reader.IsFinished())
            {
                var b = reader.ReadByte();
                var g = reader.ReadByte();
                var r = reader.ReadByte();
                var a = reader.ReadByte();
                colors[i++] = new Color(r, g, b, a);
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

        private static PooledArray<ushort> ParseIndices(IBinaryReader reader, int size, PooledArray<WorldMapObjectPoly> polygons)
        {
            PooledArray<ushort> indices = new PooledArray<ushort>(size / 2);
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

        public Byte4Array fogIds;                // ids in MFOG
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
            fogIds = reader.ReadUInt32();
            groupLiquid = reader.ReadUInt32();
            uniqueID = reader.ReadUInt32();
            flags2 = reader.ReadUInt32();
            unk = reader.ReadUInt32();
        }
    }

    public struct WorldMapObjectLiquid
    {
        public int liquidVertsX;
        public int liquidVertsY;
        public int liquidTilesX;
        public int liquidTilesY;
        public Vector3 liquidCornerCoords;
        public ushort liquidMateriallId;
        public float[]? HeightMap;
        public uint realLiquidType;
        public LiquidVertex[]? liquidVertices;
        public byte[]? tileFlags;

        public List<Vector3> verticesCoords = new List<Vector3> ();


        public WorldMapObjectLiquid(IBinaryReader reader, WorldMapObjectGroupHeader groupHeader)
        {
            liquidVertsX = reader.ReadInt32();
            liquidVertsY = reader.ReadInt32();
            liquidTilesX = reader.ReadInt32();
            liquidTilesY = reader.ReadInt32();
            liquidCornerCoords = reader.ReadVector3();
            liquidMateriallId = reader.ReadUInt16();

            // for (int i = 0; i < liquidVertsX * liquidVertsY; i++)
            // {
            //     LiquidVertex vertex = new LiquidVertex(reader);
            // 
            // }

            liquidVertices = new LiquidVertex[liquidVertsX * liquidVertsY];

            for (int y = 0; y < liquidVertsY; y++)
            {
                var y_pos = liquidCornerCoords.Y + y * 4.1666625f;
                for (int x = 0; x < liquidVertsX; x++)
                {
                    var x_pos = liquidCornerCoords.X + x * 4.1666625f;
                    LiquidVertex vertex = new LiquidVertex(reader);
                    // vertex.xPos = x_pos;
                    // vertex.yPos = y_pos;
                    liquidVertices[x + (y * liquidVertsX)] = vertex;
                    verticesCoords.Add(new Vector3(x_pos, y_pos, vertex.Height));
                }
            }

            tileFlags = new byte[liquidTilesX * liquidTilesY];

            for (int i = 0; i < (liquidTilesX * liquidTilesY); i++)
            {
                tileFlags[i] = reader.ReadByte();
            }

            // get liquid type
            uint wmoLiquidType = 0;
            if ((groupHeader.flags & 0x4) > 0)
            {
                wmoLiquidType = groupHeader.groupLiquid;
            }
            else // legacy liquid type
            {
                foreach (var tileFlag in tileFlags)
                {
                    if ((tileFlag & 15) != 15) // liquid type is in the first 4 bits. if 15 = don't render.
                        wmoLiquidType = (uint)GetLegacyWaterType(tileFlag & 15);
                }
                
            }
            realLiquidType = FromWmoLiquidType(wmoLiquidType, groupHeader);

        }

        public struct LiquidVertex
        {
            public bool IsWater = true;
            public float Height = 0.0f;
            public float xPos;
            public float yPos;

            // water
            public byte flow1 = 0;
            public byte flow2 = 0;
            public byte flow1Pct = 0;
            public byte filler = 0;
            // magma/slime
            public short u;
            public short v;

            public LiquidVertex(IBinaryReader reader)
            {
                var pos = reader.Offset;
                flow1 = reader.ReadByte();
                flow2 = reader.ReadByte();
                flow1Pct = reader.ReadByte();
                filler = reader.ReadByte();

                reader.Offset = pos;

                u = reader.ReadInt16();
                v = reader.ReadInt16();

                Height = reader.ReadFloat();
            }
        }

        private uint FromWmoLiquidType(uint basicLiquidType, WorldMapObjectGroupHeader groupHeader)
        {
            // Convert simplified WMO liquid type IDs to real LiquidType.dbc IDs
            uint realLiquidType = 0;

            if (basicLiquidType < 20)
            {
                if (basicLiquidType == 0 || basicLiquidType == 1)
                    realLiquidType = (uint)((groupHeader.flags & 0x80000) > 0 ? 14 : 13);
                else if (basicLiquidType == 1)
                    realLiquidType = 14;
                else if (basicLiquidType == 2)
                    realLiquidType = 19;
                else if (basicLiquidType == 3)
                    realLiquidType = 20;
                else if (basicLiquidType == 15)
                    realLiquidType = 17;
            }
            else
                realLiquidType = basicLiquidType + 1;

            return realLiquidType;
        }

        private int GetLegacyWaterType(int LegacyliquidType)
        {
            // from blizzard's decompiled code...
            int realLiquidType = LegacyliquidType += 1;

            if ( (realLiquidType - 1) <= 0x13)
            {
                var newwater = (realLiquidType - 1) & 3;
                if (newwater == 1)
                    return 14;
                if (newwater >= 1)
                {
                    if (newwater == 2)
                        realLiquidType = 19;
                    else if (newwater == 3)
                        realLiquidType = 20;

                    return realLiquidType;
                }
                realLiquidType = 13;

            }

            return realLiquidType;
        }

    }
}