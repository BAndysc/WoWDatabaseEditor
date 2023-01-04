using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public class BLP
    {
        public BLPHeader Header { get; }
        public Rgba32[][] Data { get; }
        public uint RealWidth { get; }
        public uint RealHeight { get; }
    
        public BLP(byte[] bytes, int start, int length, int maxSize = 0)
        {
            var reader = new MemoryBinaryReader(bytes, start, length);
            Header = new BLPHeader(reader);
            uint w = Header.Width;
            uint h = Header.Height;
            int mips = (int)Header.MipCount;
            int mipStart = 0;
            // there are multiple mip levels, so depending on max size, let's skip the biggest
            if (maxSize > 0 && Header.Mips != MipmapLevelAndFlagType.MipsNone)
            {
                while (w > maxSize || h > maxSize && w > 2 && h > 2 && mips >= 2)
                {
                    w /= 2;
                    h /= 2;
                    mips--;
                    mipStart++;
                }
            }

            RealWidth = w;
            RealHeight = h;
            Data = new Rgba32[mips][];

            int mipLevel;
            for (mipLevel = 0; mipLevel < mips; ++mipLevel)
            {
                var realFileMipLevel = mipLevel + mipStart;
                reader.Offset = (int)Header.GetMipOffset(realFileMipLevel);
                var mipReader = new LimitedReader(reader, (int)Header.GetMipSize(realFileMipLevel));

                if (Header.ColorEncoding == ColorEncoding.Palette)
                {
                    uint len = w * h;
                    Data[mipLevel] = new Rgba32[w * h];
                    for (int j = 0; j < len; j++)
                    {
                        var b = mipReader.ReadByte();

                        var color = Header.PaletteBGRA![b];
                        var offset = reader.Offset;
                        var alpha = GetAlpha(mipReader, j, len);
                        reader.Offset = offset;
                        Data[mipLevel][j] = new Rgba32(color.R, color.G, color.B, alpha);
                    }
                }
                else if (Header.ColorEncoding == ColorEncoding.Dxt)
                {
                    DXTDecompression.DXTFlags flag = (Header.AlphaDepth > 1) ? ((Header.Format == PixelFormat.Dxt5) ? DXTDecompression.DXTFlags.DXT5 : DXTDecompression.DXTFlags.DXT3) : DXTDecompression.DXTFlags.DXT1;
                    Data[mipLevel] = DXTDecompression.DecompressImage((int)w, (int)h, bytes.AsSpan(reader.Offset, mipReader.Size), flag);
                }
                else if (Header.ColorEncoding == ColorEncoding.Argb8888)
                {
                    Data[mipLevel] = new Rgba32[w * h];
                    int j = 0;
                    while (!mipReader.IsFinished())
                    {
                        Data[mipLevel][j++] = new Rgba32(mipReader.ReadByte(), mipReader.ReadByte(), mipReader.ReadByte(),
                            mipReader.ReadByte());
                    }
                }
                else
                    throw new Exception("Unknown BLP color encoding format: " + Header.ColorEncoding);
            
                w /= 2;
                h /= 2;

                if (w == 0 || h == 0)
                    break;
            }

            if ((w == 0 || h == 0) && mipLevel < mips)
            {
                var trimmed = new Rgba32[mipLevel][];
                Array.Copy(Data, trimmed, mipLevel);
                Data = trimmed;
            }
        }

        private byte GetAlpha(IBinaryReader reader, int index, uint alphaStart)
        {
            switch (Header.AlphaDepth)
            {
                default:
                    return 0xFF;
                case 1:
                {
                    reader.Offset = (int)alphaStart + (index / 8);
                    byte b = reader.ReadByte();
                    return (byte)((b & (0x01 << (index % 8))) == 0 ? 0x00 : 0xff);
                }
                case 4:
                {
                    reader.Offset = (int)alphaStart + (index / 2);
                    byte b = reader.ReadByte();
                    return (byte)(index % 2 == 0 ? (b & 0x0F) << 4 : b & 0xF0);
                }
                case 8:
                    reader.Offset = (int)alphaStart + index;
                    return reader.ReadByte();
            }
        }
    
        public enum ColorEncoding : byte
        {
            Jpeg = 0,
            Palette = 1,
            Dxt = 2,
            Argb8888 = 3,
            Argb8888_dup = 4
        }
    
        public enum PixelFormat : byte {
            Dxt1 = 0,
            Dxt3 = 1,
            Argb8888 = 2,
            Argb1555 = 3,
            Argb4444 = 4,
            Rgb565 = 5,
            A8 = 6,
            Dxt5 = 7,
            Unspecified = 8,
            Argb2565 = 9,
            Bc5 = 11
        }
    
        public enum MipmapLevelAndFlagType : byte // MIPS_TYPE
        {
            MipsNone = 0x0,
            MipsGenerated = 0x1,
            MipsHandmade = 0x2, // not supported
            FlagsMipmapMask = 0xF, // level
            FlagsUnk0X10 = 0x10,
        }
    }

    public static class BlpExtensions
    {
        public static void SaveToPng(this BLP blp, string path, int mipLevel)
        {
            int width = (int)blp.RealWidth;
            int height = (int)blp.RealHeight;
            for (int i = 0; i < mipLevel; ++i)
            {
                width /= 2;
                height /= 2;
            }
            using Image<Rgba32> img = new Image<Rgba32>(width, height);
            {
                img.ProcessPixelRows(x =>
                {
                    for (int y = 0; y < x.Height; ++y)
                    {
                        var span = x.GetRowSpan(y);
                        blp.Data[mipLevel].AsSpan(y * width, width).CopyTo(span);
                    }
                });
            }
            img.SaveAsPng(path);
        }
    }

// Some Helper Struct to store Color-Data
    public struct ARGBColor8
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        /// <summary>
        /// Converts the given Pixel-Array into the BGRA-Format
        /// This will also work vice versa
        /// </summary>
        /// <param name="pixel"></param>
        public static void ConvertToBGRA(byte[] pixel)
        {
            byte tmp = 0;
            for (int i = 0; i < pixel.Length; i += 4)
            {
                tmp = pixel[i]; // store red
                pixel[i] = pixel[i + 2]; // Write blue into red
                pixel[i + 2] = tmp; // write stored red into blue
            }
        }
    }

    public unsafe struct BLPHeader
    {
        private static uint MAGIC = 0x32504C42;
    
        public uint Version { get; }
        public BLP.ColorEncoding ColorEncoding { get; }
        public byte AlphaDepth { get; }
        public BLP.PixelFormat Format { get; }
        public BLP.MipmapLevelAndFlagType Mips { get; }
        public uint Width { get; }
        public uint Height { get; }
        public fixed uint MipOffsets[16] ;
        public fixed uint MipSizes[16] ;
        public uint MipCount { get; }
        public readonly Rgba32[]? PaletteBGRA { get; }

        public BLPHeader(IBinaryReader reader)
        {
            var magic = reader.ReadUInt32();
            if (magic != MAGIC)
                throw new Exception("Invalid blp file");
            Version = reader.ReadUInt32();
            ColorEncoding = (BLP.ColorEncoding)reader.ReadByte();
            AlphaDepth = reader.ReadByte();
            Format = (BLP.PixelFormat)reader.ReadByte();
            Mips = (BLP.MipmapLevelAndFlagType)reader.ReadByte();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            for (int i = 0; i < 16; ++i)
                MipOffsets[i] = reader.ReadUInt32();

            MipCount = 0;
            for (int i = 0; i < 16; ++i)
            {
                MipSizes[i] = reader.ReadUInt32();
                if (MipSizes[i] != 0)
                    MipCount++;
            }

            if (ColorEncoding == BLP.ColorEncoding.Palette)
            {
                PaletteBGRA = new Rgba32[256];
                for (int i = 0; i < 256; i++)
                {
                    PaletteBGRA[i].B = reader.ReadByte();
                    PaletteBGRA[i].G = reader.ReadByte();
                    PaletteBGRA[i].R = reader.ReadByte();
                    PaletteBGRA[i].A = reader.ReadByte();
                }
            }
            else
                PaletteBGRA = null;
        }

        public uint GetMipOffset(int mipLevel)
        {
            return MipOffsets[mipLevel];
        }

        public uint GetMipSize(int mipLevel)
        {
            return MipSizes[mipLevel];
        }
    }
}