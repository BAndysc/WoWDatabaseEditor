using System.Diagnostics;
using System.Threading.Tasks;
using TheMaths;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public readonly struct WorldMapObjectPlacementData
    {
        public readonly string WmoPath;
        public readonly Vector3 AbsolutePosition;
        public readonly Vector3 Rotation;
        public readonly CAaBox Bounds;

        public WorldMapObjectPlacementData(IBinaryReader reader, string[] names)
        {
            WmoPath = names[reader.ReadInt32()];
            var uniqueId = reader.ReadInt32();
            AbsolutePosition = reader.ReadVector3();
            Rotation = reader.ReadVector3();
            Bounds = CAaBox.Read(reader);
            reader.Offset += 8; // unused
        }
    }

    public readonly struct M2PlacementData
    {
        public readonly string M2Path;
        public readonly Vector3 AbsolutePosition;
        public readonly Vector3 Rotation;
        public readonly float Scale;
        public readonly ushort Flags;

        public M2PlacementData(IBinaryReader reader, string[] names)
        {
            M2Path = names[reader.ReadInt32()];
            var uniqueId = reader.ReadInt32();
            AbsolutePosition = reader.ReadVector3();
            Rotation = reader.ReadVector3();
            Scale = reader.ReadUInt16() / 1024.0f;
            Flags = reader.ReadUInt16();
        }
    }

    public readonly struct ADTSplat
    {
        public readonly uint TextureId { get; }
        public readonly uint Flags { get; }
        public readonly uint Offset { get; }
        public readonly uint Effect { get; } 

        public bool UseAlphamap => (Flags & 256) > 0;
        public bool UseAlphamapCompression => (Flags & 512) > 0;

        public ADTSplat(IBinaryReader reader)
        {
            TextureId = reader.ReadUInt32();
            Flags = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
            Effect = reader.ReadUInt32();
        }
    }

    public class AdtChunk
    {
        public Vector3 BasePosition { get; }
        public float[] Heights { get; } = new float[9 * 9 + 8 * 8];
        public Vector3[] Normals { get; } = new Vector3[9 * 9 + 8 * 8];
        public bool[,]? Holes { get; } = null;
        public byte[,,] SplatMap { get; }
        public ShadowMap? ShadowMap { get; } = null;
        public ADTSplat[] Splats { get; } = new ADTSplat[4];
        public uint AreaId { get; }
    
        public AdtChunk(IBinaryReader reader, WdtFlags? wdtFlags)
        {
            var chunkFlags = reader.ReadInt32();
            var ix = reader.ReadInt32();
            var iy = reader.ReadInt32();
            var nLayers = reader.ReadInt32();
            var nDoodadRefs = reader.ReadInt32();
            var offsMCVT = reader.ReadInt32();
            var offsMCNR = reader.ReadInt32();
            var offsMCLY = reader.ReadInt32();
            var offsMCRF = reader.ReadInt32();
            var offsMCAL = reader.ReadInt32();
            var sizeMCAL = reader.ReadInt32();
            var offsMCSH = reader.ReadInt32();
            var sizeMCSH = reader.ReadInt32();
            AreaId = reader.ReadUInt32();
            var nMapObjRefs = reader.ReadInt32();
            var holesLowRes = reader.ReadUInt16();
        
            reader.ReadBytes(42); // rest of the header
            BasePosition = reader.ReadVector3();
            reader.ReadBytes(12); // rest of header

            reader.Offset = offsMCVT;
            for (int i = 0; i < 9 * 9 + 8 * 8; ++i)
                Heights[i] = reader.ReadFloat();

            reader.Offset = offsMCNR;
            for (int i = 0; i < 9 * 9 + 8 * 8; ++i)
            {
                var x = (sbyte)reader.ReadByte();
                var y = (sbyte)reader.ReadByte();
                var z = (sbyte)reader.ReadByte();
                Normals[i] = new Vector3(x / 127.0f, y / 127.0f, z / 127.0f);
            }

            reader.Offset = offsMCLY;
            int splatId = 0;
            while (splatId < nLayers)
            {
                Splats[splatId++] = new ADTSplat(reader);
            }
        
            if (holesLowRes != 0)
            {
                Holes = new bool[4, 4];
                Holes[0, 0] = (holesLowRes & 0x1) != 0;
                Holes[1, 0] = (holesLowRes & 0x2) != 0;
                Holes[2, 0] = (holesLowRes & 0x4) != 0;
                Holes[3, 0] = (holesLowRes & 0x8) != 0;
                Holes[0, 1] = (holesLowRes & 0x10) != 0;
                Holes[1, 1] = (holesLowRes & 0x20) != 0;
                Holes[2, 1] = (holesLowRes & 0x40) != 0;
                Holes[3, 1] = (holesLowRes & 0x80) != 0;
                Holes[0, 2] = (holesLowRes & 0x100) != 0;
                Holes[1, 2] = (holesLowRes & 0x200) != 0;
                Holes[2, 2] = (holesLowRes & 0x400) != 0;
                Holes[3, 2] = (holesLowRes & 0x800) != 0;
                Holes[0, 3] = (holesLowRes & 0x1000) != 0;
                Holes[1, 3] = (holesLowRes & 0x2000) != 0;
                Holes[2, 3] = (holesLowRes & 0x4000) != 0;
                Holes[3, 3] = (holesLowRes & 0x8000) != 0;
            }

            if (offsMCAL > 0)
            {
                SplatMap = new byte[64, 64, nLayers];
                int k = 0;
                for (int i = 0; i < nLayers; ++i)
                {
                    if (!Splats[i].UseAlphamap)
                        continue;

                    reader.Offset = offsMCAL + (int)Splats[i].Offset;
                    uint length = i < nLayers - 1 ? Splats[i + 1].Offset - Splats[i].Offset : (uint)sizeMCAL - Splats[i].Offset;

                    if (Splats[i].UseAlphamapCompression)
                    {
                        int offO = 0;

                        while (offO < 4096)
                        {
                            // fill or copy mode
                            byte b = reader.ReadByte();
                        
                            bool fill = (b & 0x80) > 0;
                            int n = b & 0x7F;
                            if (n == 0)
                                break;
                            
                            b = reader.ReadByte();
                        
                            for (int kk = 0; kk < n && offO < 4096; kk++)
                            {
                                SplatMap[offO % 64, offO / 64, k] = b;
                                offO++;
                                if (!fill)
                                {
                                    if (kk < n - 1)
                                        b = reader.ReadByte();
                                }
                            }
                        }
                    }
                    else if ((wdtFlags & WdtFlags.AdtHasBigAlpha) != 0) // 4096 uncompressed mode  if 0x4 or 0x80 set
                    {
                        for (int y = 0; y < 64; ++y)
                        {
                            for (int x = 0; x < 64; ++x)
                            {
                                SplatMap[x, y, k] = reader.ReadByte();
                            }
                        }
                    }
                    else //if (length != 4096) // if 2048 uncompressed mode
                    {
                        for (int y = 0; y < 64; ++y)
                        {
                            for (int x = 0; x < 64; x += 2)
                            {
                                var val = reader.ReadByte();
                                SplatMap[x, y, k] = (byte)((val & 0xF) * 0x10);
                                SplatMap[x + 1, y, k] = (byte)(((val >> 4) & 0xF) * 0x10);
                            }
                        }

                        if ((chunkFlags & 0x8000) == 0) // Do not fix alpha map flag
                        {
                            for (int j = 0; j < 64; ++j)
                            {
                                SplatMap[j, 63, k] = SplatMap[j, 62, k];
                                SplatMap[63, j, k] = SplatMap[62, j, k];
                            }
                            SplatMap[63, 63, k] = SplatMap[62, 62, k];
                        }
                    }

                    ++k;
                }
            }

            if (offsMCSH > 0 && sizeMCSH > 0)
            {
                ShadowMap = new ShadowMap();
                reader.Offset = offsMCSH;
                for (int y = 0; y < 64; ++y)
                {
                    for (int x = 0; x < 64; x += 8)
                    {
                        var b = reader.ReadByte();
                        ShadowMap[x + 0, y] = (b & 0b00000001) != 0;
                        ShadowMap[x + 1, y] = (b & 0b00000010) != 0;
                        ShadowMap[x + 2, y] = (b & 0b00000100) != 0;
                        ShadowMap[x + 3, y] = (b & 0b00001000) != 0;
                        ShadowMap[x + 4, y] = (b & 0b00010000) != 0;
                        ShadowMap[x + 5, y] = (b & 0b00100000) != 0;
                        ShadowMap[x + 6, y] = (b & 0b01000000) != 0;
                        ShadowMap[x + 7, y] = (b & 0b10000000) != 0;
                    }
                }
            }

        }
    }

    public struct FastAdtAreaTable
    {
        public uint[,] AreaIds { get; } = new uint[16, 16];
        public FastAdtAreaTable(IBinaryReader reader)
        {
            int y = 0;
            int x = 0;
            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                if (chunkName == "MCNK")
                {
                    AreaIds[y, x] = ReadChunkAreaId(reader);

                    x++;
                    if (x == 16)
                    {
                        x = 0;
                        y++;
                    }
                }
            
                reader.Offset = offset + size;
            }
        }

        private uint ReadChunkAreaId(IBinaryReader reader)
        {
            reader.Offset += 0x34; // skip part of the header
            return reader.ReadUInt32();
        }
    }

    // memory optimized storage for wow shadow map (64 x 64 bits)
    public class ShadowMap
    {
        private ulong[] rows = new ulong[64];

        public bool this[int y, int x]
        {
            get
            {
                var row = rows[y];
                return (row & ((ulong)1 << x)) > 0;
            }
            set
            {
                var row = rows[y];
                if (value)
                    row |= (ulong)1 << x;
                else
                    row &= ~((ulong)1 << x);
                rows[y] = row;
            }
        }
    }

    public class MH2OLiquidChunk
    {
        // private int OffsetInstances { get; }
        // private uint LayerCount { get; }
        // private int OffsetAttributes { get; }
        public MH2OLiquidInstance[]? LiquidInstances; // array size = layerCount
        public MH2OChunkAttributes[]? ChunkAttributes; // is it an array too ?

        public MH2OLiquidChunk(IBinaryReader reader)
        {
            var OffsetInstances = reader.ReadInt32();
            var LayerCount = reader.ReadUInt32();
            var OffsetAttributes = reader.ReadInt32();

            if (OffsetInstances > 0)
            {
                reader.Offset = OffsetInstances;
                LiquidInstances = new MH2OLiquidInstance[LayerCount];
            }

            if (OffsetAttributes > 0)
            {
                reader.Offset = OffsetAttributes;
                ChunkAttributes = new MH2OChunkAttributes[1]; // not 1 per layer ?
            }

        }
    }

    public readonly struct MH2OLiquidInstance
    {
        public readonly ushort LiquidTypeId { get; } // LiquidType.dbc
        public readonly ushort LiquidVertexFormat { get; } // LiquidVertexFormat if wrath, liquid_object_or_lvf if cata+
        public readonly float MinHeightLevel { get; }
        public readonly float MaxHeightLevel { get; }
        public readonly byte X_Offset { get; }
        public readonly byte Y_Offset { get; }
        public readonly byte Width { get; }
        public readonly byte Height { get; }
        public readonly int OffsetExistsBitmap { get; }
        public readonly int OffsetVertexData { get; }

        // TODO : struct union ?
        public readonly LiquidVertexFormat0 Format0VertexList;
        public readonly LiquidVertexFormat1 Format1VertexList;
        public readonly LiquidVertexFormat2 Format2VertexList;
        public readonly LiquidVertexFormat3 Format3VertexList;

        public readonly byte[]? RenderBitMap; // not all tiles in the instances need to be filled. always (width * height + 7) / 8 bytes. offset can be 0 for all-exist

        public MH2OLiquidInstance(IBinaryReader reader)
        {
            LiquidTypeId = reader.ReadUInt16();
            LiquidVertexFormat = reader.ReadUInt16();
            MinHeightLevel = reader.ReadFloat();
            MaxHeightLevel = reader.ReadFloat();
            X_Offset = reader.ReadByte();
            Y_Offset = reader.ReadByte();
            Width = reader.ReadByte();
            Height = reader.ReadByte();
            OffsetExistsBitmap = reader.ReadInt32();
            OffsetVertexData = reader.ReadInt32();

            if (OffsetExistsBitmap > 0) // TODO
            {
                var bytecount = (Width * Height + 7) / 8;
                reader.Offset = OffsetExistsBitmap;

                RenderBitMap = new byte[bytecount];

                for (int i = 0; i < bytecount; i++)
                    RenderBitMap[i] = reader.ReadByte();
            }

            if (OffsetVertexData == 0 && LiquidTypeId != 2) // if offset = 0 and liquidType ≠ 2, then let LVF = 2, i.e. some ocean shit
                LiquidVertexFormat = 2;

            if (OffsetVertexData > 0)
            {

                int entriescount = (Width + 1) * (Height + 1);

                reader.Offset = OffsetVertexData;

                if (LiquidVertexFormat > 3) // default to 0 if unknown format
                    LiquidVertexFormat = 0;

                if (LiquidVertexFormat == 0)
                    Format0VertexList = new LiquidVertexFormat0(reader, entriescount);
                    // Format0VertexList.Heightmap = new float[entriescount];
                else if (LiquidVertexFormat == 1)
                    Format1VertexList = new LiquidVertexFormat1(reader, entriescount);
                else if (LiquidVertexFormat == 2)
                    Format2VertexList = new LiquidVertexFormat2(reader, entriescount);
                else if (LiquidVertexFormat == 3)
                    Format3VertexList = new LiquidVertexFormat3(reader, entriescount);
                else
                {
                    // invalid vertex format
                }

            }
            else
            {
                // No vertex data. debug message here
            }

        }

        public readonly struct UvMapEntry
        {
            public readonly ushort X;
            public readonly ushort Y;

            public UvMapEntry(IBinaryReader reader)
            {
                X = reader.ReadUInt16();
                Y = reader.ReadUInt16();
            }
        }

        // Case 0, Height and Depth data. This is the go-to layout for pre-WoD (MoP?) data.
        public struct LiquidVertexFormat0
        {
            public float[] Heightmap;
            public byte[] Depthmap;

            public LiquidVertexFormat0(IBinaryReader reader, int count)
            {
                Heightmap = new float[count];
                for (int i = 0; i < count; i++)
                    Heightmap[i] = reader.ReadFloat();

                Depthmap = new byte[count];
                for (int i = 0; i < count; i++)
                    Depthmap[i] = reader.ReadByte();
            }
        }

        public struct LiquidVertexFormat1
        {
            public float[] Heightmap;
            public UvMapEntry[] Uvmap;

            public LiquidVertexFormat1(IBinaryReader reader, int count)
            {
                Heightmap = new float[count];
                for (int i = 0; i < count; i++)
                    Heightmap[i] = reader.ReadFloat();

                Uvmap = new UvMapEntry[count];
                for (int i = 0; i < count; i++)
                    Uvmap[i] = new UvMapEntry(reader);
            }
        }

        public struct LiquidVertexFormat2
        {
            public byte[] Depthmap;
            public LiquidVertexFormat2(IBinaryReader reader, int count)
            {
                Depthmap = new byte[count];
                for (int i = 0; i < count; i++)
                    Depthmap[i] = reader.ReadByte();
            }
        }

        public readonly struct LiquidVertexFormat3
        {
            public readonly float[] Heightmap;
            public readonly UvMapEntry[] Uvmap;
            public readonly byte[] Depthmap;

            public LiquidVertexFormat3(IBinaryReader reader, int count)
            {
                Heightmap = new float[count];
                for (int i = 0; i < count; i++)
                    Heightmap[i] = reader.ReadFloat();

                Uvmap = new UvMapEntry[count];
                for (int i = 0; i < count; i++)
                    Uvmap[i] = new UvMapEntry(reader);

                Depthmap = new byte[count];
                for (int i = 0; i < count; i++)
                    Depthmap[i] = reader.ReadByte();
            }
        }
    }


    public readonly struct MH2OChunkAttributes
    {
        // 8*8 bit masks.
        public readonly ulong Fishable { get; }
        public readonly ulong Deep { get; }

        public MH2OChunkAttributes(IBinaryReader reader)
        {
            Fishable = reader.ReadUInt64();
            Deep = reader.ReadUInt64();
        }
    }

    public struct SMChunkInfo
    {
        public uint Offset { get; set; }
        public uint Size { get; set; }

        public SMChunkInfo(IBinaryReader reader)
        {
            Offset = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            reader.ReadUInt32(); // flags always 0
            reader.ReadUInt32(); // padding
        }
    }

    public class ADT
    {
        public string[] Textures { get; }
        public WorldMapObjectPlacementData[] WorldMapObjects { get; }
        public M2PlacementData[] M2Objects { get; }
        public SMChunkInfo[] ChunkInfo { get; } = new SMChunkInfo[16 * 16];
        public AdtChunk[] Chunks { get; } = new AdtChunk[16 * 16];
        public MH2OLiquidChunk[] MH2OLiquidChunks { get; } = new MH2OLiquidChunk[16 * 16];

        public ADT(IBinaryReader reader, WdtFlags? wdtFlags)
        {
            string[]? worldMapObjectsIds = null;
            string[]? m2Ids = null;
            Dictionary<int, string>? wmosNameOffsets = null;
            Dictionary<int, string>? m2NameOffsets = null;
            int chunkId = 0;
            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                var partialReader = new LimitedReader(reader, size);

                if (chunkName == "MTEX")
                    Textures = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, false, out _);
                else if (chunkName == "MCIN")
                {
                    for (int i = 0; i < 16 * 16; ++i)
                        ChunkInfo[i] = new SMChunkInfo(partialReader);
                    Debug.Assert(partialReader.IsFinished());
                }
                else if (chunkName == "MWMO")
                    ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out wmosNameOffsets);
                else if (chunkName == "MWID")
                    worldMapObjectsIds = ReadNameOffsets(partialReader, wmosNameOffsets);
                else if (chunkName == "MODF")
                    WorldMapObjects = ReadWorldMapObjectPlacementData(partialReader, worldMapObjectsIds);
                else if (chunkName == "MMDX")
                    ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out m2NameOffsets);
                else if (chunkName == "MMID")
                    m2Ids = ReadNameOffsets(partialReader, m2NameOffsets);
                else if (chunkName == "MDDF")
                    M2Objects = ReadM2PlacementData(partialReader, m2Ids);
                else if (chunkName == "MH2O")
                {
                    MH2OLiquidChunks[chunkId++] = new MH2OLiquidChunk(partialReader);
                }

                else if (chunkName == "MCNK")
                {
                    chunkId = 0;
                    Debug.Assert(ChunkInfo[chunkId].Offset == offset - 8);
                    Debug.Assert(ChunkInfo[chunkId].Size == size + 8);
                    Chunks[chunkId++] = new AdtChunk(partialReader, wdtFlags);
                }
            
                reader.Offset = offset + size;
            }
        }

        private M2PlacementData[] ReadM2PlacementData(LimitedReader reader, string[]? names)
        {
            Debug.Assert(names != null);
            int i = 0;
            M2PlacementData[] data = new M2PlacementData[reader.Size / 0x24];
            while (!reader.IsFinished())
            {
                data[i++] = new M2PlacementData(reader, names);
            }
            return data;
        }
    
        private WorldMapObjectPlacementData[] ReadWorldMapObjectPlacementData(LimitedReader reader, string[]? names)
        {
            Debug.Assert(names != null);
            int i = 0;
            WorldMapObjectPlacementData[] data = new WorldMapObjectPlacementData[reader.Size / 0x40];
            while (!reader.IsFinished())
            {
                data[i++] = new WorldMapObjectPlacementData(reader, names);
            }
            return data;
        }

        private string[] ReadNameOffsets(LimitedReader reader, Dictionary<int,string>? wmosNameOffsets)
        {
            Debug.Assert(wmosNameOffsets != null);
            int i = 0;
            string[] names = new string[reader.Size / 4];
            while (!reader.IsFinished())
            {
                names[i++] = wmosNameOffsets[reader.ReadInt32()];
            }

            return names;
        }
    }
}