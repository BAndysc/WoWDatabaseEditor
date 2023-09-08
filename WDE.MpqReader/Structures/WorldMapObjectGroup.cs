using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;
using TheMaths;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public class WorldMapObjectGroup : System.IDisposable
    {
        public readonly WorldMapObjectGroupHeader Header;
        public readonly PooledArray<WorldMapObjectPoly> Polygons;
        public readonly PooledArray<ushort> Indices;
        public readonly PooledArray<Vector3> Vertices;
        public readonly PooledArray<Color>? VertexColors;
        public readonly PooledArray<Color>? VertexColors2;
        public readonly ushort[] CollisionOnlyIndices;
        public readonly PooledArray<Vector3> Normals;
        public readonly List<PooledArray<Vector2>> UVs = new();
        public readonly WorldMapObjectBatch[]? Batches;
        public readonly WorldMapObjectLiquid Liquid;
        public readonly CAaBspNode[]? BspNodes;
        public readonly ushort[]? BspIndices;
    
        public WorldMapObjectGroup(IBinaryReader reader, in WMOHeader wmoHeader)
        {
            var firstChunkName = reader.ReadUInt32();
            Debug.Assert(firstChunkName == 0x4d564552);//'R' && firstChunkName[1] == 'E' && firstChunkName[2] == 'V' && firstChunkName[3] == 'M');
            reader.ReadInt32();
            reader.ReadInt32(); // version

            var secondChunkName = reader.ReadUInt32();
            Debug.Assert(secondChunkName == 0x4d4f4750, "secondChunkName == " + secondChunkName);// == 'P' && secondChunkName[1] == 'G' && secondChunkName[2] == 'O' && secondChunkName[3] == 'M');
            reader.ReadInt32();
            Header = new WorldMapObjectGroupHeader(reader);

            ushort[]? mobr = null;
            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                var partialReader = new LimitedReader(reader, size);

                if (chunkName == "MOPY")
                    Polygons = ParsePolygons(partialReader, size);
                else if (chunkName == "MOVI")
                {
                    if (Indices != null)
                        throw new Exception("Duplicate MOVT chunk");
                    Indices = ParseIndices(partialReader, size, Polygons);
                }
                else if (chunkName == "MOVT")
                {
                    if (Vertices != null)
                        throw new Exception("Duplicate MOVT chunk");
                    Vertices = ReadVectors3(partialReader, size);
                }
                else if (chunkName == "MONR")
                    Normals = ReadVectors3(partialReader, size);
                else if (chunkName == "MOTV")
                    UVs.Add(ReadVectors2(partialReader, size));
                else if (chunkName == "MOBA")
                    Batches = ReadBatches(partialReader, size);
                else if (chunkName == "MOCV")
                {
                    var colors = ReadVertexColors(partialReader, size);
                    if (VertexColors == null)
                        VertexColors = colors;
                    else
                        VertexColors2 = colors;
                }
                else if (chunkName == "MLIQ")
                    Liquid = new WorldMapObjectLiquid(reader, in wmoHeader, in Header);
                else if (chunkName == "MORB")
                    throw new Exception("MORB not supported");
                else if (chunkName == "MOBN")
                    BspNodes = ReadBspNodes(reader, size);
                else if (chunkName == "MOBR")
                    mobr = ReadBspIndices(reader, size);

                CollisionOnlyIndices = BuildCollisionOnlyIndices(Polygons, Indices);
                
                reader.Offset = offset + size;
            }

            if (mobr != null && Indices != null)
            {
                BspIndices = new ushort[mobr.Length * 3];
                for (var i = 0; i < mobr.Length; i++) {
                    BspIndices[i*3 + 0] = Indices[3*mobr[i]+0];
                    BspIndices[i*3 + 1] = Indices[3*mobr[i]+1];
                    BspIndices[i*3 + 2] = Indices[3*mobr[i]+2];
                }   
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

        private static ushort[] ReadBspIndices(IBinaryReader reader, int size)
        {
            ushort[] indices = new ushort[size / Unsafe.SizeOf<ushort>()];
            for (int i = 0; i < indices.Length; ++i)
                indices[i] = reader.ReadUInt16();
            return indices;
        }

        private static CAaBspNode[] ReadBspNodes(IBinaryReader reader, int size)
        {
            CAaBspNode[] nodes = new CAaBspNode[size / Unsafe.SizeOf<CAaBspNode>()];
            for (int i = 0; i < nodes.Length; ++i)
                nodes[i] = new CAaBspNode(reader);
            return nodes;
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
            VertexColors2?.Dispose();
            Polygons.Dispose();
            Indices.Dispose();
            Vertices.Dispose();
            Normals.Dispose();
            foreach (var uv in UVs)
                uv.Dispose();
        }
    }

    public readonly struct WorldMapObjectPoly
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

        public readonly Flags flags;
        public readonly byte materialId;

        public bool IsCollisionOnly => materialId == 0xFF;
        
        public WorldMapObjectPoly(IBinaryReader reader)
        {
            flags = (Flags)reader.ReadByte();
            materialId = reader.ReadByte();
        }
    }
    
    public enum CAaBspNodeFlags
    {
        Flag_XAxis = 0x0,
        Flag_YAxis = 0x1,
        Flag_ZAxis = 0x2,
        Flag_AxisMask = 0x3,
        Flag_Leaf = 0x4,
        Flag_NoChild = 0xFFFF,
    };
    
    public readonly struct CAaBspNode
    {
        public readonly CAaBspNodeFlags flags;
        public readonly short negChild;      // index of bsp child node (right in this array)
        public readonly short posChild;
        public readonly ushort nFaces;       // num of triangle faces in MOBR
        public readonly int faceStart;    // index of the first triangle index(in MOBR)
        public readonly float planeDist;

        public CAaBspNode(IBinaryReader reader)
        {
            flags = (CAaBspNodeFlags)reader.ReadUInt16();
            negChild = reader.ReadInt16();
            posChild = reader.ReadInt16();
            nFaces = reader.ReadUInt16();
            faceStart = reader.ReadInt32();
            planeDist = reader.ReadFloat();
        }
    }

    public enum WorldMapObjectBatchFlags
    {
        flag_use_material_id_large = 2,
    }

    public readonly struct WorldMapObjectBatch
    {
        public readonly ushort bx, by, bz; // a bounding box for culling, see "unknown_box" below
        public readonly short tx, ty, tz;
        public readonly uint startIndex;                     // index of the first face index used in MOVI
        public readonly ushort count; // number of MOVI indices used
        public readonly ushort minIndex; // index of the first vertex used in MOVT
        public readonly ushort maxIndex; // index of the last vertex used (batch includes this one)
        public readonly WorldMapObjectBatchFlags flags;
        public readonly ushort material_id; // index in MOMT

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
            flags = (WorldMapObjectBatchFlags)reader.ReadByte();
            material_id = reader.ReadByte();
            if (flags.HasFlagFast(WorldMapObjectBatchFlags.flag_use_material_id_large))
                material_id = (ushort)tz; // that's how wow encodes it, crazy, ain't it?
        }
    }

    [Flags]
    public enum WorldMapObjectGroupFlags
    {
        HasBSPTree = 0x1,
        HasLightMapUnused = 0x2,
        HasVertexColors = 0x4,
        Exterior = 0x8,
        ExteriorLit = 0x40,
        Unreachable = 0x80,
        ShowSkyInInterior = 0x100,
        HasLights = 0x200,
        HasDoodads = 0x800,
        HasWater = 0x1000,
        Interior = 0x2000,
        QueryMountAllowed = 0x8000,
        AlwaysDraw = 0x10000,
        ShowSkybox = 0x40000,
        IsNotWaterButOcean = 0x80000,
        IsMountAllowed = 0x200000,
        HasSeconMOCV = 0x1000000,
        HasTwoMOTV = 0x2000000,
        Antiportal = 0x4000000,
        ExteriorCull = 0x20000000,
        HasThreeMOTB = 0x40000000
    }

    public readonly struct WorldMapObjectGroupHeader
    {
        public readonly uint groupName;               // offset into MOGN
        public readonly uint descriptiveGroupName;    // offset into MOGN
        public readonly WorldMapObjectGroupFlags flags;
        public readonly CAaBox boundingBox;              // as with flags, same as in corresponding MOGI entry

        public readonly ushort portalStart;             // index into MOPR
        public readonly ushort portalCount;             // number of MOPR items used after portalStart

        public readonly ushort transBatchCount;
        public readonly ushort intBatchCount;
        public readonly ushort extBatchCount;
        public readonly ushort padding_or_batch_type_d; // probably padding, but might be data?

        public readonly Byte4Array fogIds;                // ids in MFOG
        public readonly uint groupLiquid;             // see below in the MLIQ chunk

        public readonly uint uniqueID;
        public readonly uint flags2;
        public readonly uint unk;                     // UNUSED: 20740

        public WorldMapObjectGroupHeader(IBinaryReader reader)
        {
            groupName = reader.ReadUInt32();
            descriptiveGroupName = reader.ReadUInt32();
            flags = (WorldMapObjectGroupFlags)reader.ReadUInt32();
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

    public readonly struct WorldMapObjectLiquid
    {
        public readonly int liquidVertsX;
        public readonly int liquidVertsY;
        public readonly int liquidTilesX;
        public readonly int liquidTilesY;
        public readonly Vector3 liquidCornerCoords;
        public readonly ushort liquidMateriallId;
        public readonly uint realLiquidType;
        public readonly byte[]? tileFlags;
        public readonly LiquidVertex[] vertices;
        public readonly bool isOneHeight;

        public WorldMapObjectLiquid(IBinaryReader reader, in WMOHeader wmoHeader, in WorldMapObjectGroupHeader groupHeader)
        {
            liquidVertsX = reader.ReadInt32();
            liquidVertsY = reader.ReadInt32();
            liquidTilesX = reader.ReadInt32();
            liquidTilesY = reader.ReadInt32();
            liquidCornerCoords = reader.ReadVector3();
            liquidMateriallId = reader.ReadUInt16();

            isOneHeight = true;
            float? previousSize = null;
            vertices = new LiquidVertex[liquidVertsX * liquidVertsY];
            for (int y = 0; y < liquidVertsY; y++)
            {
                for (int x = 0; x < liquidVertsX; x++)
                {
                    LiquidVertex vertex = new LiquidVertex(reader);
                    if (previousSize.HasValue && Math.Abs(previousSize.Value - vertex.Height) > 0.001f)
                        isOneHeight = false;
                    vertices[y * liquidVertsX + x] = vertex;
                    
                    previousSize = vertex.Height;
                }
            }
            
            tileFlags = new byte[liquidTilesX * liquidTilesY];
            for (int i = 0; i < (liquidTilesX * liquidTilesY); i++)
                tileFlags[i] = reader.ReadByte();

            // get liquid type
            uint wmoLiquidType = 0;
            if (wmoHeader.flags.HasFlagFast(WorldMapObjectFlags.FlagUseLiquidTypeDbcId))
            {
                wmoLiquidType = groupHeader.groupLiquid;
            }
            else // legacy liquid type
            {
                if (tileFlags.Length > 0)
                {
                    if ((tileFlags[0] & 15) != 15) // liquid type is in the first 4 bits. if 15 = don't render.
                    {
                        wmoLiquidType = (uint)GetLegacyWaterType(tileFlags[0] & 15);
                    }
                }
            }
            realLiquidType = FromWmoLiquidType(wmoLiquidType, in groupHeader);
        }

        public readonly struct LiquidVertex
        {
            public readonly float Height;

            // water
            public readonly byte flow1 = 0;
            public readonly byte flow2 = 0;
            public readonly byte flow1Pct = 0;
            public readonly byte filler = 0;
            // magma/slime
            public short u => (short)(flow1 | (flow2 << 8));
            public short v => (short)(flow1Pct | (filler << 8));

            public LiquidVertex(IBinaryReader reader)
            {
                flow1 = reader.ReadByte();
                flow2 = reader.ReadByte();
                flow1Pct = reader.ReadByte();
                filler = reader.ReadByte();
                Height = reader.ReadFloat();
            }
        }

        private static uint FromWmoLiquidType(uint basicLiquidType, in WorldMapObjectGroupHeader groupHeader)
        {
            // Convert simplified WMO liquid type IDs to real LiquidType.dbc IDs
            uint realLiquidType = 0;

            if (basicLiquidType < 20)
            {
                if (basicLiquidType == 0 || basicLiquidType == 1)
                    realLiquidType = (uint)(groupHeader.flags.HasFlagFast(WorldMapObjectGroupFlags.IsNotWaterButOcean) ? 14 : 13);
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

        private static int GetLegacyWaterType(int LegacyliquidType)
        {
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
