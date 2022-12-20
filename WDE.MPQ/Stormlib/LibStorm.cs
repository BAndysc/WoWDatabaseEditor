using System.Runtime.InteropServices;

namespace WDE.MPQ.Stormlib;

public static class LibStorm
{
    private const string STORMLIB = "storm";
    
    [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool SFileOpenArchive([MarshalAs(UnmanagedType.LPStr)] string szParams, uint priority, uint flags, out IntPtr phStorage);
    
    [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool SFileCloseArchive(IntPtr hFile);
    
    [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool SFileOpenPatchArchive(
        IntPtr hMpq,                      // Handle to an open MPQ (primary MPQ)
        [MarshalAs(UnmanagedType.LPStr)] string szMpqName,           // Patch archive file name
        [MarshalAs(UnmanagedType.LPStr)] string  szPatchPathPrefix,   // Prefix for patch file names
        uint dwFlags                     // Reserved
    );
    
    [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool SFileOpenFileEx(IntPtr hStorage, [MarshalAs(UnmanagedType.LPStr)] string pvFileName, uint searchScope, out IntPtr PtrFileHandle);
    
    [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool SFileReadFile(
        IntPtr hFile,                           // Handle to an open file
        byte[] lpBuffer,                        // Buffer where to read the file data
        uint dwToRead,                         // Number of bytes to be read
        out uint pdwRead,                           // Optional pointer to store number of bytes that were actually read,
        IntPtr overlappedStruct
    );
    
    [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool SFileCloseFile(IntPtr hFile);

    [DllImport(STORMLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern uint SFileGetFileSize(IntPtr hFile, out uint high);

    public static bool FileReadFile(
        IntPtr hFile, // Handle to an open file
        byte[] lpBuffer, // Buffer where to read the file data
        uint dwToRead, // Number of bytes to be read
        out uint pdwRead  // Optional pointer to store number of bytes that were actually read,
    )
    {
        return SFileReadFile(hFile, lpBuffer, dwToRead, out pdwRead, IntPtr.Zero);
    }
}