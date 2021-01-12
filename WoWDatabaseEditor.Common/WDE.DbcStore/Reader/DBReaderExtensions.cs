using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WDBXEditor.Reader
{
    public static class DBReaderExtensions
    {
        public static string ReadStringNull(this BinaryReader reader)
        {
            byte num;
            var temp = new List<byte>();

            while ((num = reader.ReadByte()) != 0)
                temp.Add(num);

            return Encoding.UTF8.GetString(temp.ToArray());
        }

        public static sbyte[] ReadSByte(this BinaryReader br, int count)
        {
            var arr = new sbyte[count];
            for (var i = 0; i < count; i++)
                arr[i] = br.ReadSByte();

            return arr;
        }

        public static byte[] ReadByte(this BinaryReader br, int count)
        {
            var arr = new byte[count];
            for (var i = 0; i < count; i++)
                arr[i] = br.ReadByte();

            return arr;
        }

        public static int[] ReadInt32(this BinaryReader br, int count)
        {
            var arr = new int[count];
            for (var i = 0; i < count; i++)
                arr[i] = br.ReadInt32();

            return arr;
        }

        public static uint[] ReadUInt32(this BinaryReader br, int count)
        {
            var arr = new uint[count];
            for (var i = 0; i < count; i++)
                arr[i] = br.ReadUInt32();

            return arr;
        }

        public static float[] ReadSingle(this BinaryReader br, int count)
        {
            var arr = new float[count];
            for (var i = 0; i < count; i++)
                arr[i] = br.ReadSingle();

            return arr;
        }

        public static long[] ReadInt64(this BinaryReader br, int count)
        {
            var arr = new long[count];
            for (var i = 0; i < count; i++)
                arr[i] = br.ReadInt64();

            return arr;
        }

        public static ulong[] ReadUInt64(this BinaryReader br, int count)
        {
            var arr = new ulong[count];
            for (var i = 0; i < count; i++)
                arr[i] = br.ReadUInt64();

            return arr;
        }

        public static string ReadString(this BinaryReader br, int count)
        {
            byte[] stringArray = br.ReadBytes(count);
            return Encoding.UTF8.GetString(stringArray);
        }

        public static int ReadInt32(this BinaryReader br, FieldStructureEntry map = null)
        {
            if (map == null)
                return br.ReadInt32();

            var b = new byte[sizeof(int)];
            for (var i = 0; i < map.ByteCount; i++)
                b[i] = br.ReadByte();

            return BitConverter.ToInt32(b, 0);
        }

        public static uint ReadUInt32(this BinaryReader br, FieldStructureEntry map = null)
        {
            if (map == null)
                return br.ReadUInt32();

            var b = new byte[sizeof(uint)];
            for (var i = 0; i < map.ByteCount; i++)
                b[i] = br.ReadByte();

            return BitConverter.ToUInt32(b, 0);
        }

        public static long ReadInt64(this BinaryReader br, FieldStructureEntry map = null)
        {
            if (map == null)
                return br.ReadInt64();

            var b = new byte[sizeof(long)];
            for (var i = 0; i < map.ByteCount; i++)
                b[i] = br.ReadByte();

            return BitConverter.ToInt64(b, 0);
        }

        public static ulong ReadUInt64(this BinaryReader br, FieldStructureEntry map = null)
        {
            if (map == null)
                return br.ReadUInt64();

            var b = new byte[sizeof(ulong)];
            for (var i = 0; i < map.ByteCount; i++)
                b[i] = br.ReadByte();

            return BitConverter.ToUInt64(b, 0);
        }

        public static void Scrub(this BinaryReader br, long pos)
        {
            br.BaseStream.Position = pos;
        }

        public static void Scrub(this BinaryWriter br, long pos)
        {
            br.BaseStream.Position = pos;
        }


        public static void WriteInt32(this BinaryWriter bw, int value, FieldStructureEntry map = null)
        {
            if (map == null)
                bw.Write(value);
            else
                bw.Write(BitConverter.GetBytes(value), 0, map.ByteCount);
        }

        public static void WriteUInt32(this BinaryWriter bw, uint value, FieldStructureEntry map = null)
        {
            if (map == null)
                bw.Write(value);
            else
                bw.Write(BitConverter.GetBytes(value), 0, map.ByteCount);
        }

        public static void WriteInt64(this BinaryWriter bw, long value, FieldStructureEntry map = null)
        {
            if (map == null)
                bw.Write(value);
            else
                bw.Write(BitConverter.GetBytes(value), 0, map.ByteCount);
        }

        public static void WriteUInt64(this BinaryWriter bw, ulong value, FieldStructureEntry map = null)
        {
            if (map == null)
                bw.Write(value);
            else
                bw.Write(BitConverter.GetBytes(value), 0, map.ByteCount);
        }

        public static void WriteArray<T>(this BinaryWriter bw, T[] data)
        {
            var bytes = new byte[data.Length * Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
            bw.Write(bytes);
        }
    }
}