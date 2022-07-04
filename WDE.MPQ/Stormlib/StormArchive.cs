using WDE.Common.MPQ;

namespace WDE.MPQ.Stormlib;

public class StormArchive : IMpqArchive
{
    private readonly string path;
    private IntPtr handle;

    public StormArchive(string path)
    {
        this.path = path;
        if (!LibStorm.SFileOpenArchive(path, 0, 0x00010000 | 0x00000100, out handle))
            throw new Exception("Can't open MPQ!");
    }
    
    public void Dispose()
    {
        LibStorm.SFileCloseArchive(handle);
        handle = IntPtr.Zero;
    }

    public int? ReadFile(byte[] buffer, int size, string path)
    {
        if (!LibStorm.SFileOpenFileEx(handle, path.ToUpper(), 0, out var file))
            return null;
        if (!LibStorm.FileReadFile(file, buffer, (uint)size, out var bytesRead))
        {
            LibStorm.SFileCloseFile(file);
            return null;
        }
        LibStorm.SFileCloseFile(file);
        return (int)bytesRead;
    }

    public int? GetFileSize(string path)
    {
        if (!LibStorm.SFileOpenFileEx(handle, path.ToUpper(), 0, out var file))
            return null;
        var low = LibStorm.SFileGetFileSize(file, out var high);
        if (high != 0)
            throw new Exception("Unexpected file size >= 4GB");
        LibStorm.SFileCloseFile(file);
        return (int)low;
    }

    public IMpqArchive Clone()
    {
        return new StormArchive(path);
    }
    
    public MpqLibrary Library => MpqLibrary.Stormlib;
}