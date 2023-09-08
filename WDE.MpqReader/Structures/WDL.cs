using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures;

public class WDL
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MareChunk
    {
        public unsafe fixed short OuterHeights[17 * 17];
        public unsafe fixed short InnerHeights[16 * 16];
    }

    public MareChunk[] flatChunks = Array.Empty<MareChunk>();
    public int[,] ChunkIndices = new int[64, 64]; // -1 if not present
    
    public bool HasChunk(int y, int x) => ChunkIndices[y, x] != -1;
    public ref MareChunk GetChunk(int y, int x) => ref flatChunks[ChunkIndices[y, x]];
    public int NonEmptyChunks => flatChunks.Length;
    
    public WDL(IBinaryReader reader)
    {
        if (reader.ReadChunkName() != "MVER")
            throw new Exception("Invalid WDL file");

        reader.ReadUInt32();
        reader.ReadUInt32();

        while (!reader.IsFinished())
        {
            var chunkName = reader.ReadChunkName();
            var size = reader.ReadInt32();

            if (chunkName == "MAOF")
            {
                var partialReader = new LimitedReader(reader, size);
                Trace.Assert(size == 64 * 64 * 4);
                List<MareChunk> chunks = new List<MareChunk>();
                for (int y = 0; y < 64; ++y)
                {
                    for (int x = 0; x < 64; ++x)
                    {
                        var offset = partialReader.ReadInt32();
                        if (offset == 0)
                        {
                            ChunkIndices[y, x] = -1;
                            continue;
                        }
                        
                        var originalOffset = partialReader.Offset;
                        reader.Offset = offset;
                        
                        chunkName = reader.ReadChunkName();
                        size = reader.ReadInt32();
                        Trace.Assert(chunkName == "MARE");
                        Trace.Assert(size == (17 * 17 + 16 * 16) * 2);

                        ChunkIndices[y, x] = chunks.Count;
                        chunks.Add(reader.ReadStruct<MareChunk>());
                        partialReader.Offset = originalOffset;
                    }
                }
                flatChunks = chunks.ToArray();
            }
            else
            {
                reader.Offset += size;
            }
        }
    }
}