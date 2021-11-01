// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System;

namespace WDE.MPQ.Parsing
{
    [Flags]
    enum CompressionFlags : byte
    {
        HuffmanEncoded = 0x01,
        Deflated = 0x02,
        Imploded = 0x08,
        Bzip2 = 0x10,
        SparseCompressed = 0x20,
        IMA_ADPCM_Mono = 0x40,
        IMA_ADPCM_Stereo = 0x80,
    }
}