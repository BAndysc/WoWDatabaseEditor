using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WDBXEditor.Reader
{
    public class MemoryReader : IDisposable
    {
        public ulong BaseAddress { get; private set; }
        public IntPtr ProcessHandle { get; private set; }

        private Process process;

        public MemoryReader(Process proc)
        {
            if ((proc?.Id ?? 0) == 0)
                throw new Exception("Invalid process");

            BaseAddress = (ulong)proc.MainModule.BaseAddress;
            ProcessHandle = OpenProcess(ProcessAccess.AllAccess, false, proc.Id);

            if (ProcessHandle == IntPtr.Zero)
                throw new Win32Exception("Failed to open the process for reading.");

            Process.EnterDebugMode();
            process = proc;
        }

        public void Dispose()
        {
            if (ProcessHandle != IntPtr.Zero && !CloseHandle(ProcessHandle))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            GC.SuppressFinalize(this);
        }

        ~MemoryReader()
        {
            Dispose();
        }

        public byte[] ReadBytes(IntPtr address, uint count)
        {
            var buffer = new byte[count];
            int bytesRead;
            if (!ReadProcessMemory(ProcessHandle, address, buffer, count, out bytesRead))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (bytesRead != count)
                throw new Exception("Bytes read did not match required byte count!");
            return buffer;
        }

        public T Read<T>(IntPtr address) where T : struct
        {
            object ret = default(T);
            var buffer = new byte[0];

            if (typeof(T) == typeof(string))
                return (T)(object)ReadCString(address);
            else
                buffer = ReadBytes(address, (uint)Marshal.SizeOf(typeof(T)));

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Object:
                    GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    Marshal.PtrToStructure(handle.AddrOfPinnedObject(), ret);
                    handle.Free();
                    break;
                case TypeCode.Boolean:
                    ret = BitConverter.ToBoolean(buffer, 0);
                    break;
                case TypeCode.Char:
                    ret = BitConverter.ToChar(buffer, 0);
                    break;
                case TypeCode.Byte:
                    ret = buffer[0];
                    break;
                case TypeCode.Int16:
                    ret = BitConverter.ToInt16(buffer, 0);
                    break;
                case TypeCode.UInt16:
                    ret = BitConverter.ToUInt16(buffer, 0);
                    break;
                case TypeCode.Int32:
                    ret = BitConverter.ToInt32(buffer, 0);
                    break;
                case TypeCode.UInt32:
                    ret = BitConverter.ToUInt32(buffer, 0);
                    break;
                case TypeCode.Int64:
                    ret = BitConverter.ToInt64(buffer, 0);
                    break;
                case TypeCode.UInt64:
                    ret = BitConverter.ToUInt64(buffer, 0);
                    break;
                case TypeCode.Single:
                    ret = BitConverter.ToSingle(buffer, 0);
                    break;
                case TypeCode.Double:
                    ret = BitConverter.ToDouble(buffer, 0);
                    break;
                default:
                    throw new NotSupportedException($"Unknown type {typeof(T).Name}.");
            }

            return (T)ret;
        }

        public string ReadCString(IntPtr address)
        {
            var buffer = new List<byte>();

            int i = 0;
            byte current = Read<byte>((IntPtr)(address.ToInt32() + i));
            while (current != 0)
            {
                buffer.Add(current);
                i++;
                current = Read<byte>((IntPtr)(address.ToInt32() + i));
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        #region PInvokes

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private enum ProcessAccess
        {
            AllAccess = 0x2 | 0x40 | 0x400 | 0x200 | 0x1 | 0x8 | 0x10 | 0x20 | 0x100000
        }
        #endregion
    }
}
