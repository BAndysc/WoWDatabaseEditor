using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using WDE.Common.Utils;
using WDE.PacketViewer.PacketParserIntegration;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels;

public interface IPacketViewModelStore : IDisposable
{
    bool Load(DumpFormatType dumpFormatType);
    Task<string> GetTextAsync(PacketViewModel packet);
    string GetText(PacketViewModel packet);
    FastByteSearcher.OpenedFileSearcher OpenFileSearcher(FastByteSearcher searcher);
    bool PacketContainsText(ref FastByteSearcher.OpenedFileSearcher searcher, PacketViewModel packet);
}

public class NullPacketViewModelStore : IPacketViewModelStore
{
    public bool Load(DumpFormatType dumpFormatType) => true;

    public Task<string> GetTextAsync(PacketViewModel packet) => Task.FromResult(GetText(packet));

    public string GetText(PacketViewModel packet) => packet.Packet.BaseData.StringData.ToString() ?? "";

    public FastByteSearcher.OpenedFileSearcher OpenFileSearcher(FastByteSearcher searcher)
    {
        return default;
    }

    public bool PacketContainsText(ref FastByteSearcher.OpenedFileSearcher searcher, PacketViewModel packet)
    {
        return false;
    }

    public void Dispose()
    {
    }
}

public class PacketViewModelStore : IPacketViewModelStore
{
    private AsyncMonitor asyncMonitor = new();
    private FileStream? textFileStream;
    private long length;
    private string txtFile;

    private int? cachedPacketId;
    private string? cachedText;

    public PacketViewModelStore(string sniffFile)
    {
        if (sniffFile.EndsWith(".pkt") || sniffFile.EndsWith(".bin") || sniffFile.EndsWith(".dat"))
            sniffFile = sniffFile.Substring(0, sniffFile.Length - 4);
        
        txtFile = sniffFile + "_parsed.txt";
    }
    
    ~PacketViewModelStore()
    {
        Dispose();
    }

    public bool Load(DumpFormatType dumpFormatType)
    {
        Dispose();
        if (dumpFormatType == DumpFormatType.UniversalProtoWithSeparateText)
        {
            if (File.Exists(txtFile))
            {
                textFileStream = File.OpenRead(txtFile);
                length = textFileStream.Length;
                return true;
            }

            return false;
        }
        return true;
    }
    
    public async Task<string> GetTextAsync(PacketViewModel packet)
    {
        return await GetTextAsync(packet.Packet.BaseData);
    }

    public string GetText(PacketViewModel packet)
    {
        return GetText(packet.Packet.BaseData);
    }

    private string GetText(long start, int length)
    {
        using var _ = asyncMonitor.Enter();
        textFileStream!.Seek(start, SeekOrigin.Begin);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        length = textFileStream.Read(buffer, 0, length);
        var text = Encoding.UTF8.GetString(buffer.AsSpan(0, length));
        ArrayPool<byte>.Shared.Return(buffer);
        return text;
    }

    private async Task<string> GetTextAsync(long start, int length)
    {
        using var _ = await asyncMonitor.EnterAsync();
        textFileStream!.Seek(start, SeekOrigin.Begin);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        length = await textFileStream.ReadAsync(buffer, 0, length);
        var text = Encoding.UTF8.GetString(buffer.AsSpan(0, length));
        ArrayPool<byte>.Shared.Return(buffer);
        return text;
    }

    private unsafe string GetText(PacketBase baseData)
    {
        if (baseData.TextStartOffset == null)
        {
            return baseData.StringData.ToString() ?? "";
        }
        else if (textFileStream != null && baseData.TextStartOffset + baseData.TextLength < length)
        {
            var text = GetText(baseData.TextStartOffset.Value, baseData.TextLength);
            if (baseData.HeaderTextStartOffset is { } headerStart && headerStart + baseData.HeaderTextLength < length)
            {
                var header = GetText(headerStart, baseData.HeaderTextLength);
                return header + text;
            }
            return text;
        }

        return "";
    }

    private async Task<string> GetTextAsync(PacketBase baseData)
    {
        if (baseData.TextStartOffset == null)
            return baseData.StringData.ToString() ?? "";

        if (textFileStream != null && baseData.TextStartOffset + baseData.TextLength < length)
        {
            var text = await GetTextAsync(baseData.TextStartOffset.Value, baseData.TextLength);
            if (baseData.HeaderTextStartOffset is { } headerStart && headerStart + baseData.HeaderTextLength < length)
            {
                var header = await GetTextAsync(headerStart, baseData.HeaderTextLength);
                return header + text;
            }
            return text;
        }

        return "";
    }

    public FastByteSearcher.OpenedFileSearcher OpenFileSearcher(FastByteSearcher searcher)
    {
        return searcher.OpenFile(txtFile);
    }

    public bool PacketContainsText(ref FastByteSearcher.OpenedFileSearcher searcher, PacketViewModel packet)
    {
        ref var baseData = ref packet.Packet.BaseData;
        if (baseData.HeaderTextStartOffset.HasValue && baseData.IsFirstPacketWithThisHeader)
        {
            if (searcher.FindOne(baseData.HeaderTextStartOffset.Value, baseData.HeaderTextLength, false).HasValue)
                return true;
        }
        if (baseData.TextStartOffset.HasValue)
        {
            if (searcher.FindOne(baseData.TextStartOffset.Value, baseData.TextLength, false).HasValue)
                return true;
        }
        return false;
    }

    public void Dispose()
    {
        textFileStream?.Dispose();
        length = 0;
        textFileStream = null;
    }
}