using System;
using SixLabors.ImageSharp.PixelFormats;

namespace WDE.MpqReader.Structures
{
    public static class DXTDecompression
    {
        [Flags]
        public enum DXTFlags
        {
            DXT1 = 1 << 0,
            DXT3 = 1 << 1,
            DXT5 = 1 << 2,
            // Additional Enums not implemented :o
        }

        private static void Decompress(Span<byte> rgba, ReadOnlySpan<byte> block, int blockIndex, DXTFlags flags)
        {
            // get the block locations
            int colorBlockIndex = blockIndex;

            if ((flags & (DXTFlags.DXT3 | DXTFlags.DXT5)) != 0)
                colorBlockIndex += 8;

            // decompress color
            DecompressColor(rgba, block, colorBlockIndex, (flags & DXTFlags.DXT1) != 0);

            // decompress alpha separately if necessary
            if ((flags & DXTFlags.DXT3) != 0)
                DecompressAlphaDxt3(rgba, block, blockIndex);
            else if ((flags & DXTFlags.DXT5) != 0)
                DecompressAlphaDxt5(rgba, block, blockIndex);
        }

        private static void DecompressAlphaDxt3(Span<byte> rgba, ReadOnlySpan<byte> block, int blockIndex)
        {
            // Unpack the alpha values pairwise
            for (int i = 0; i < 8; i++)
            {
                // Quantise down to 4 bits
                byte quant = block[blockIndex + i];

                byte lo = (byte)(quant & 0x0F);
                byte hi = (byte)(quant & 0xF0);

                // Convert back up to bytes
                rgba[8 * i + 3] = (byte)(lo | (lo << 4));
                rgba[8 * i + 7] = (byte)(hi | (hi >> 4));
            }
        }

        private static void DecompressAlphaDxt5(Span<byte> rgba, ReadOnlySpan<byte> block, int blockIndex)
        {
            // Get the two alpha values
            byte alpha0 = block[blockIndex + 0];
            byte alpha1 = block[blockIndex + 1];

            // compare the values to build the codebook
            Span<byte> codes = stackalloc byte[8];
            codes[0] = alpha0;
            codes[1] = alpha1;
            if (alpha0 <= alpha1)
            {
                // Use 5-Alpha Codebook
                for (int i = 1; i < 5; i++)
                    codes[1 + i] = (byte)(((5 - i) * alpha0 + i * alpha1) / 5);
                codes[6] = 0;
                codes[7] = 255;
            }
            else
            {
                // Use 7-Alpha Codebook
                for (int i = 1; i < 7; i++)
                {
                    codes[i + 1] = (byte)(((7 - i) * alpha0 + i * alpha1) / 7);
                }
            }

            // decode indices
            Span<byte> indices = stackalloc byte[16];
            int blockSrc_pos = 2;
            int indices_pos = 0;
            for (int i = 0; i < 2; i++)
            {
                // grab 3 bytes
                int value = 0;
                for (int j = 0; j < 3; j++)
                {
                    int _byte = block[blockIndex + blockSrc_pos++];
                    value |= (_byte << 8 * j);
                }

                // unpack 8 3-bit values from it
                for (int j = 0; j < 8; j++)
                {
                    int index = (value >> 3 * j) & 0x07;
                    indices[indices_pos++] = (byte)index;
                }
            }

            // write out the indexed codebook values
            for (int i = 0; i < 16; i++)
            {
                rgba[4 * i + 3] = codes[indices[i]];
            }
        }

        private static void DecompressColor(Span<byte> rgba, ReadOnlySpan<byte> block, int blockIndex, bool isDxt1)
        {
            // Unpack Endpoints
            Span<byte> codes = stackalloc byte[16];
            int a = Unpack565(block, blockIndex, 0, codes, 0);
            int b = Unpack565(block, blockIndex, 2, codes, 4);

            // generate Midpoints
            for (int i = 0; i < 3; i++)
            {
                int c = codes[i];
                int d = codes[4 + i];

                if (isDxt1 && a <= b)
                {
                    codes[8 + i] = (byte)((c + d) / 2);
                    codes[12 + i] = 0;
                }
                else
                {
                    codes[8 + i] = (byte)((2 * c + d) / 3);
                    codes[12 + i] = (byte)((c + 2 * d) / 3);
                }
            }

            // Fill in alpha for intermediate values
            codes[8 + 3] = 255;
            codes[12 + 3] = (isDxt1 && a <= b) ? (byte)0 : (byte)255;

            // unpack the indices
            Span<byte> indices = stackalloc byte[16];
            for (int i = 0; i < 4; i++)
            {
                byte packed = block[blockIndex + 4 + i];

                indices[0 + i * 4] = (byte)(packed & 0x3);
                indices[1 + i * 4] = (byte)((packed >> 2) & 0x3);
                indices[2 + i * 4] = (byte)((packed >> 4) & 0x3);
                indices[3 + i * 4] = (byte)((packed >> 6) & 0x3);
            }

            // store out the colours
            for (int i = 0; i < 16; i++)
            {
                int offset = 4 * indices[i];

                rgba[4 * i + 0] = codes[offset + 0];
                rgba[4 * i + 1] = codes[offset + 1];
                rgba[4 * i + 2] = codes[offset + 2];
                rgba[4 * i + 3] = codes[offset + 3];
            }
        }

        private static int Unpack565(ReadOnlySpan<byte> block, int blockIndex, int packed_offset, Span<byte> colour, int colour_offset)
        {
            // Build packed value
            int value = block[blockIndex + packed_offset] | (block[blockIndex + 1 + packed_offset] << 8);

            // get components in the stored range
            byte red = (byte)((value >> 11) & 0x1F);
            byte green = (byte)((value >> 5) & 0x3F);
            byte blue = (byte)(value & 0x1F);

            // Scale up to 8 Bit
            colour[0 + colour_offset] = (byte)((red << 3) | (red >> 2));
            colour[1 + colour_offset] = (byte)((green << 2) | (green >> 4));
            colour[2 + colour_offset] = (byte)((blue << 3) | (blue >> 2));
            colour[3 + colour_offset] = 255;

            return value;
        }

        public static Rgba32[] DecompressImage(int width, int height, ReadOnlySpan<byte> data, DXTFlags flags)
        {
            Rgba32[] rgba = new Rgba32[width * height];

            // initialise the block input
            int sourceBlock_pos = 0;
            int bytesPerBlock = (flags & DXTFlags.DXT1) != 0 ? 8 : 16;
            Span<byte> targetRGBA = stackalloc byte[4 * 16];

            // loop over blocks
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    // decompress the block
                    int targetRGBA_pos = 0;

                    if (data.Length == sourceBlock_pos)
                        continue;

                    Decompress(targetRGBA, data, sourceBlock_pos, flags);

                    // Write the decompressed pixels to the correct image locations
                    for (int py = 0; py < 4; py++)
                    {
                        for (int px = 0; px < 4; px++)
                        {
                            int sx = x + px;
                            int sy = y + py;
                            if (sx < width && sy < height)
                            {
                                int targetPixel = (width * sy + sx);

                                rgba[targetPixel + 0] = new Rgba32(targetRGBA[targetRGBA_pos + 0],
                                    targetRGBA[targetRGBA_pos + 1],
                                    targetRGBA[targetRGBA_pos + 2],
                                    targetRGBA[targetRGBA_pos + 3]) ;
                                targetRGBA_pos += 4;
                            }
                            else
                            {
                                // Ignore that pixel
                                targetRGBA_pos += 4;
                            }
                        }
                    }
                    sourceBlock_pos += bytesPerBlock;
                }
            }
            return rgba;
        }
    }
}