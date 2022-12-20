using WDE.Common.MPQ;

namespace WDE.MPQ.Casc;

public class CascArchive : IMpqArchive
{
    private readonly string path;
    private IntPtr handle;

    public CascArchive(string path)
    {
        this.path = path;
        if (!LibCasc.CascOpenStorage(path, 0, out handle))
            throw new Exception("Can't open CASC!");
    }
    
    public void Dispose()
    {
        LibCasc.CascCloseStorage(handle);
        handle = IntPtr.Zero;
    }

    public int? ReadFile(byte[] buffer, int size, string path)
    {
        if (!LibCasc.CascOpenFile(handle, path, 0, 0, out var file))
            return null;
        if (!LibCasc.CascReadFile(file, buffer, (uint)size, out var bytesRead))
        {
            LibCasc.CascCloseFile(file);
            return null;
        }
        LibCasc.CascCloseFile(file);
        return (int)bytesRead;
    }

    public int? GetFileSize(string path)
    {
        if (!LibCasc.CascOpenFile(handle, path, 0, 0, out var file))
            return null;
        if (!LibCasc.CascGetFileSize64(file, out var fileSize))
        {
            LibCasc.CascCloseFile(file);
            return null;
        }
        LibCasc.CascCloseFile(file);
        return (int)fileSize;
    }

    public IMpqArchive Clone()
    {
        return new CascArchive(path);
    }

    public MpqLibrary Library => MpqLibrary.Casclib;
}