using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using WDE.PacketViewer.PacketParserIntegration;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels;

public interface IPacketViewModelStore : IDisposable
{
    bool Load(DumpFormatType dumpFormatType);
    Task<string> GetTextAsync(PacketViewModel packet);
    string GetText(PacketViewModel packet);
}

public class NullPacketViewModelStore : IPacketViewModelStore
{
    public bool Load(DumpFormatType dumpFormatType) => true;

    public Task<string> GetTextAsync(PacketViewModel packet) => Task.FromResult(GetText(packet));

    public string GetText(PacketViewModel packet) => packet.Packet.BaseData.StringData.ToString() ?? "";

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
        var text = await GetTextAsync(packet.Packet.BaseData);
        if (packet.Packet.KindCase == PacketHolder.KindOneofCase.UpdateObject)
            return ExtractUpdate(packet, text);
        return text;
    }

    public string GetText(PacketViewModel packet)
    {
        if (packet.Packet.KindCase == PacketHolder.KindOneofCase.UpdateObject)
        {
            if (packet.Id != cachedPacketId)
            {
                cachedPacketId = packet.Id;
                cachedText = GetText(packet.Packet.BaseData);
            }
            return ExtractUpdate(packet, cachedText!);   
        }
        return GetText(packet.Packet.BaseData);
    }

    private string GetFirstThreeLines(string text)
    {
        int indexOfFirstLine = text.IndexOf("\n", StringComparison.Ordinal);
        if (indexOfFirstLine == -1)
            return text;
        int indexOfSecondLine = text.IndexOf("\n", indexOfFirstLine + 1, StringComparison.Ordinal);
        if (indexOfSecondLine == -1)
            return text;
        int indexOfThirdLine = text.IndexOf("\n", indexOfSecondLine + 1, StringComparison.Ordinal);
        if (indexOfThirdLine == -1)
            return text;
        return text.Substring(0, indexOfThirdLine);
    }

    private string ExtractUpdate(PacketViewModel vm, string text)
    {
        var update = vm.Packet.UpdateObject;
        var count = update.Created.Count + update.Destroyed.Count + update.Updated.Count + update.OutOfRange.Count;
        if (count != 1)
            return text;
        int? start = null;
        int? len = null;
        if (update.Created.Count == 1)
        {
            start = update.Created[0].TextStartOffset.Value;
            len = update.Created[0].TextLength.Value;
        }
        else if (update.Destroyed.Count == 1)
        {
            start = update.Destroyed[0].TextStartOffset.Value;
            len = update.Destroyed[0].TextLength.Value;
        }
        else if (update.Updated.Count == 1)
        {
            start = update.Updated[0].TextStartOffset.Value;
            len = update.Updated[0].TextLength.Value;
        }
        else if (update.OutOfRange.Count == 1)
        {
            start = update.OutOfRange[0].TextStartOffset.Value;
            len = update.OutOfRange[0].TextLength.Value;
        }
        if (start.HasValue && len.HasValue && start.Value + len.Value < text.Length)
        {
            return GetFirstThreeLines(text) + "\n" + text.Substring(start.Value, len.Value);
        }
        return " - file error - . Have you edited the _parsed.txt file?";
    }
    
    private unsafe string GetText(PacketBase baseData)
    {
        if (baseData.TextStartOffset == null)
        {
            return baseData.StringData.ToString() ?? "";
        }
        else if (textFileStream != null && baseData.TextLength != null &&
                 baseData.TextStartOffset->Value + baseData.TextLength->Value < length)
        {
            using var _ = asyncMonitor.Enter();
            textFileStream.Seek(baseData.TextStartOffset->Value, SeekOrigin.Begin);
            var len = (int)(baseData.TextLength->Value);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(len);
            textFileStream.Read(buffer, 0, len);
            var text = Encoding.UTF8.GetString(buffer.AsSpan(0, len));
            ArrayPool<byte>.Shared.Return(buffer);
            return text;
        }

        return "";
    }

    
    private Task<string> GetTextAsync(PacketBase baseData)
    {
        unsafe
        {
            if (baseData.TextStartOffset == null)
            {
                return Task.FromResult(baseData.StringData.ToString() ?? "");
            }
        }

        async Task<string> AsyncWork(long start, int length)
        {
            using var _ = await asyncMonitor.EnterAsync();
            textFileStream.Seek(start, SeekOrigin.Begin);
            var len = (int)(length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(len);
            await textFileStream.ReadAsync(buffer, 0, len);
            var text = Encoding.UTF8.GetString(buffer.AsSpan(0, len));
            ArrayPool<byte>.Shared.Return(buffer);
            return text;
        }

        unsafe
        {
            if (textFileStream != null && baseData.TextLength != null && baseData.TextStartOffset->Value + baseData.TextLength->Value < length)
            {
                return AsyncWork(baseData.TextStartOffset->Value, baseData.TextLength->Value);
            }
        }

        return Task.FromResult("");
    }

    public void Dispose()
    {
        textFileStream?.Dispose();
        length = 0;
        textFileStream = null;
    }
}