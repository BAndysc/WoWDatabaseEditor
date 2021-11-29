using TheMaths;
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
        AdtHasUpsideDownGround = 32
        // TODO : MOP+ flags
    }

    public class WDTChunk
    {
        public uint chunkFlags { get; }

        public WDTChunk(IBinaryReader reader)
        {
            chunkFlags = reader.ReadUInt32();
        }
    }
    
    public class WDT
    {
        public uint Version { get; }
        public WDTHeader Header { get; }
        public WDTChunk[] Chunks { get; } = new WDTChunk[4096];
        public string Mwmo { get; }
        public MODF WorldMapObject { get; }

        public WDT(IBinaryReader reader)
        {
            int chunkId = 0;
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
                    Chunks[chunkId++] = new WDTChunk(partialReader);
                else if (chunkName == "MWMO")
                    Mwmo = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out mwmosNameOffsets)[0];
                else if (chunkName == "MODF")
                    WorldMapObject = MODF.Read(partialReader);
                reader.Offset = offset + size;
            }
        }
    }

    public class WDTHeader
    {
        public WdtFlags flags { get; init; }
        public uint unknown { get; init; }
        public uint[] unused { get; init; }

        private WDTHeader() { }

        public static WDTHeader Read(IBinaryReader reader)
        {
            return new WDTHeader()
            {
                flags = (WdtFlags)reader.ReadUInt32(),
                unknown = reader.ReadUInt32(),
                unused = new uint[6]
                {
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32()
                }
            };
        }
    }

    public class MODF
    {
        public uint uniqueId { get; init; }
        public Vector3 pos { get; init; }
        public Vector3 rot { get; init; }
        public CAaBox extents { get; init; }
        public uint flags { get; init; } // TODO
        public uint doodadSet { get; init; }
        public uint nameSet { get; init; }
        public uint pad { get; init; }

        private MODF() { }

        public static MODF Read(IBinaryReader reader)
        {
            reader.ReadUInt32();
            return new MODF()
            {
                uniqueId = reader.ReadUInt32(),
                pos = reader.ReadVector3(),
                rot = reader.ReadVector3(),
                extents = CAaBox.Read(reader),
                flags = reader.ReadUInt16(),
                doodadSet = reader.ReadUInt16(),
                nameSet = reader.ReadUInt16(),
                pad = reader.ReadUInt16()
            };
        }
    }
}
