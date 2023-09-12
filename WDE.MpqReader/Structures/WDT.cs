using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using TheMaths;
using WDE.Common.MPQ;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    [Flags]
    public enum WdtFlags : ushort
    {
        WdtUsesGlobalMapObj = 1,
        AdtHasMccv = 2,
        AdtHasBigAlpha = 4,
        AdtHasDoodadRefsSortedBySizeCat = 8,
        FlagLightingVertices = 16,
        AdtHasUpsideDownGround = 32,
        AdtHasHeightTexturing = 128
        // TODO : MOP+ flags
    }

    public enum AdtChunkType
    {
        None,
        AllWater,
        Regular
    }
    
    public class WDTChunk
    {
        private static float BlockSize = 533.33333f;
        
        public uint X { get; }
        public uint Y { get; }
        public Vector3 MiddlePosition => ChunkToWoWPosition(X, Y);
        public uint chunkFlags { get; }
        public bool HasAdt => (chunkFlags & 1) == 1;
        public bool IsAllWater => (chunkFlags & 2) == 2;
        
        private static Vector3 ChunkToWoWPosition(uint x, uint y)
        {
            return new Vector3((-(int)y + 32) * BlockSize, (-(int)x + 32) * BlockSize, 0);
        }
        
        public WDTChunk(IBinaryReader reader, uint x, uint y)
        {
            chunkFlags = reader.ReadUInt32();
            reader.ReadUInt32();
            X = x;
            Y = y;
        }
    }
    
    public class WDT
    {
        public uint Version { get; }
        public WDTHeader Header { get; }
        public WDTChunk[,] Chunks { get; }
        public string? Mwmo { get; }
        public MODF? WorldMapObject { get; }

        public WDT(IBinaryReader reader, GameFilesVersion version)
        {
            Dictionary<int, string>? mwmosNameOffsets = null;
            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                var partialReader = new LimitedReader(reader, size);

                if (chunkName == "MVER")
                    Version = reader.ReadUInt32();
                else if (chunkName == "MPHD")
                    Header = WDTHeader.Read(partialReader);
                else if (chunkName == "MAIN")
                    Chunks = ReadWdtChunks(partialReader);
                else if (chunkName == "MWMO")
                    Mwmo = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out mwmosNameOffsets).FirstOrDefault();
                else if (chunkName == "MODF")
                    WorldMapObject = MODF.Read(partialReader, version);
                reader.Offset = offset + size;
            }
        }

        private WDTChunk[,] ReadWdtChunks(IBinaryReader reader)
        {
            WDTChunk[,] chunks = new WDTChunk[64, 64];
            for (uint y = 0; y < 64; ++y)
            {
                for (uint x = 0; x < 64; ++x)
                {
                    chunks[y, x] = new WDTChunk(reader, x, y);
                }
            }
            return chunks;
        }
    }

    public struct BitVector64
    {
        private BitVector32 low;
        private BitVector32 high;

        public bool this[int index]
        {
            get
            {
                if (index <= 31)
                    return low[(1<<index)];
                return high[1 << (index - 32)];
            }
            set
            {
                if (index <= 31)
                    low[(1<<index)] = value;
                else
                    high[1 << (index - 32)] = value;
            }
        }
    }
    
    public class FastWDTChunks
    {
        public BitVector64[] Chunks { get; }

        public FastWDTChunks(IBinaryReader reader)
        {
            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                var partialReader = new LimitedReader(reader, size);

                if (chunkName == "MAIN")
                {
                    Chunks = ReadWdtChunks(partialReader);
                    break;
                }
                reader.Offset = offset + size;
            }
        }

        private BitVector64[] ReadWdtChunks(IBinaryReader reader)
        {
            BitVector64[] chunks = new BitVector64[64];
            for (uint y = 0; y < 64; ++y)
            {
                for (int x = 0; x < 64; ++x)
                {
                    var chunkFlags = reader.ReadUInt32();
                    reader.ReadUInt32();
                    chunks[y][x] = (chunkFlags & 1) == 1;
                }
            }
            return chunks;
        }
    }

    public class WDTHeader
    {
        public WdtFlags flags { get; init; }
        public uint unknown { get; init; }

        private WDTHeader() { }

        public static WDTHeader Read(IBinaryReader reader)
        {
            var flags = (WdtFlags)reader.ReadUInt32();
            var unknown = reader.ReadUInt32();
            reader.Offset += 4 * 6; // padding;
            return new WDTHeader()
            {
                flags = flags,
                unknown = unknown
            };
        }
    }
    
    public enum MODFFlags {
        modf_destroyable = 0x1,         // set for destroyable buildings like the tower in DeathknightStart. This makes it a server-controllable game object.
        modf_use_lod = 0x2,             // WoD(?)+: also load _LOD1.WMO for use dependent on distance
        modf_unk_has_scale = 0x4,       // Legion+: if this flag is set then use scale = scale / 1024, otherwise scale is 1.0
        modf_entry_is_filedata_id = 0x8, // Legion+: nameId is a file data id to directly load //SMMapObjDef::FLAG_FILEDATAID
        modf_use_sets_from_mwds = 0x80  // Shadowlands+: if set, doodad set indexes of which to load should be taken from MWDS chunk
    };

    public readonly struct MODF
    {
        public readonly FileId? fileId;
        public readonly uint uniqueId;
        public readonly Vector3 pos;
        public readonly Vector3 rot;
        public readonly CAaBox extents;
        public readonly MODFFlags flags; // TODO
        public readonly uint doodadSet;
        public readonly uint nameSet;
        public readonly float scale;

        public MODF(IBinaryReader reader, GameFilesVersion version)
        {
            var entryOrFileId = reader.ReadUInt32();
            uniqueId = reader.ReadUInt32();
            pos = reader.ReadVector3();
            rot = reader.ReadVector3();
            extents = CAaBox.Read(reader);
            flags = (MODFFlags)reader.ReadUInt16();
            doodadSet = reader.ReadUInt16();
            nameSet = reader.ReadUInt16();
            scale = reader.ReadUInt16();
            if (version < GameFilesVersion.Legion_7_3_5 || !flags.HasFlagFast(MODFFlags.modf_unk_has_scale))
                scale = 1;
            else
                scale /= 1024;

            fileId = default;
            if (flags.HasFlagFast(MODFFlags.modf_entry_is_filedata_id))
                fileId = entryOrFileId;
        }

        public static MODF Read(IBinaryReader reader, GameFilesVersion version)
        {
            return new MODF(reader, version);
        }
    }
}
