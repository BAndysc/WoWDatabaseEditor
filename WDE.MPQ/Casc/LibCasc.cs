using System.Runtime.InteropServices;

namespace WDE.MPQ.Casc;

public static class LibCasc
{
    private const string CASCLIB = "casc";
    
    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool CascOpenOnlineStorage(
        [MarshalAs(UnmanagedType.LPStr)] string szParams,                     // Parameters of an online storage
        uint dwLocaleMask,                   // Locale mask. Only used for World of Warcraft storages.
        out IntPtr phStorage                    // Pointer to a HANDLE variable that receives storage handle on success
    );

    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool CascOpenStorage([MarshalAs(UnmanagedType.LPStr)] string szParams, uint dwLocaleMask, out IntPtr phStorage);
   
    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool CascOpenFile(IntPtr hStorage, [MarshalAs(UnmanagedType.LPStr)] string pvFileName, uint dwLocaleFlags, uint dwOpenFlags, out IntPtr PtrFileHandle);
    
    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool CascReadFile(
        IntPtr hFile,                           // Handle to an open file
        byte[] lpBuffer,                        // Buffer where to read the file data
        uint dwToRead,                         // Number of bytes to be read
        out uint pdwRead                           // Optional pointer to store number of bytes that were actually read
    );
    
    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool CascCloseFile(IntPtr hFile);

    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern uint CascGetFileSize(IntPtr hFile, out uint high);
    
    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool CascGetFileSize64(IntPtr hFile, out ulong fileSize);
    
    [DllImport(CASCLIB, CallingConvention = CallingConvention.Winapi, ExactSpelling = true, PreserveSig = true, SetLastError = true, ThrowOnUnmappableChar = false)]
    public static extern bool CascCloseStorage(IntPtr hStorage);
}