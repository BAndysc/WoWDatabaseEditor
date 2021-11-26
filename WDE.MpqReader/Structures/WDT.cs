using System;
using System.Collections.Generic;
using TheMaths;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    [Flags]
    public enum WDTFlags : ushort
    {
        wdt_uses_global_map_obj = 1,
        adt_has_mccv = 2,
        adt_has_big_alpha = 4,
        adt_has_doodadrefs_sorted_by_size_cat = 8,
        FLAG_LIGHTINGVERTICES = 16,
        adt_has_upside_down_ground = 32
        // TODO : MOP+ flags
    }

    public class WDTChunk
    {
        public uint chunkFlags { get; }
        public uint asyncId { get; }

        public WDTChunk(IBinaryReader reader)
        {
            chunkFlags = reader.ReadUInt32();
            asyncId = reader.ReadUInt32();
        }
    }
    
    public class WDT
    {
        public uint Version { get; }
        public WDTHeader Header { get; }
        public WDTChunk[] Chunks { get; } = new WDTChunk[4096];
        public WorldMapObjectPlacementData WorldMapObject { get; }
        public string Mwmo { get; }
        public MODF Modf { get; }

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
                    // if (size > 0) // only WMO only maps use it
                    {
                        Mwmo = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out mwmosNameOffsets)[0];
                    if (chunkName == "MODF")
                        Modf = MODF.Read(partialReader);
                        // WorldMapObject = new WorldMapObjectPlacementData();
                    }
                reader.Offset = offset + size;
            }
        }
    }

    public class WDTHeader
    {
        
        public WDTFlags flags { get; init; }
        public uint unknown { get; init; }
        public uint unused { get; init; }

        private WDTHeader() { }

        public static WDTHeader Read(IBinaryReader reader)
        {
            return new WDTHeader()
            {
                flags = (WDTFlags)reader.ReadUInt32(),
                unknown = reader.ReadUInt32(),
                unused = reader.ReadUInt32()
            };
        }
    }

    public class MODF
    {
        public uint nameId { get; init; }
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
            return new MODF()
            {
                nameId = reader.ReadUInt32(),
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
