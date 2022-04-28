// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using System;

namespace WDE.MPQ.Parsing
{
    static class Crypto
    {
        private static readonly uint[] CryptTable = new uint[0x500];

        static Crypto()
        {
            InitializeCryptTable();
        }

        public static void DecryptInPlace(byte[] data, uint seed1)
        {
            if (data == null) throw new ArgumentNullException("data");

            var seed2 = 0xEEEEEEEE;

            for (var i = 0; i < data.Length; i += sizeof (uint))
            {
                var value = BitConverter.ToUInt32(data, i);

                seed2 += CryptTable[0x400 + (seed1 & 0xff)];
                value = value ^ (seed1 + seed2);
                seed1 = ((~seed1 << 0x15) + 0x11111111) | (seed1 >> 0x0b);
                seed2 = value + seed2 + (seed2 << 5) + 3;

                data[i] = (byte)((value) & 0xFF);
                data[i + 1] = (byte)((value >> 8) & 0xFF);
                data[i + 2] = (byte)((value >> 16) & 0xFF);
                data[i + 3] = (byte)((value >> 24) & 0xFF);
            }
        }

        public static uint Hash(string str, HashType hashType)
        {
            uint seed1 = 0x7FED7FED;
            var seed2 = 0xEEEEEEEE;

            foreach (var c in str.ToUpperInvariant())
            {
                uint ch = (byte) c;
                seed1 = CryptTable[((uint) hashType*0x100) + ch] ^ (seed1 + seed2);
                seed2 = ch + seed1 + seed2 + (seed2 << 5) + 3;
            }

            return seed1;
        }

        private static void InitializeCryptTable()
        {
            uint seed = 0x00100001;

            for (uint index1 = 0; index1 < 0x100; index1++)
            {
                var i = 0;

                for (var index2 = index1; i < 5; i++, index2 += 0x100)
                {
                    seed = (seed*125 + 3)%0x2AAAAB;
                    var temp1 = (seed & 0xFFFF) << 0x10;

                    seed = (seed*125 + 3)%0x2AAAAB;
                    var temp2 = (seed & 0xFFFF);

                    CryptTable[index2] = (temp1 | temp2);
                }
            }
        }
    }
}