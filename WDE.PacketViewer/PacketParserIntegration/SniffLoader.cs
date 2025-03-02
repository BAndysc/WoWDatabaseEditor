using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProtoZeroSharp;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.PacketViewer.Settings;
using WDE.PacketViewer.Utils;
using WowPacketParser.PacketStructures;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration
{
    [AutoRegister]
    [SingleInstance]
    public class SniffLoader : ISniffLoader
    {
        private readonly IPacketParserService packetParserService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IPacketViewerSettings viewerSettings;

        public SniffLoader(IPacketParserService packetParserService,
            IMessageBoxService messageBoxService, IPacketViewerSettings viewerSettings)
        {
            this.packetParserService = packetParserService;
            this.messageBoxService = messageBoxService;
            this.viewerSettings = viewerSettings;
        }
        
        public async Task<Packets> LoadSniff(RefCountedArena allocator, string path, int? customVersion, CancellationToken token, bool withText, IProgress<float> progress)
        {
            var extension = Path.GetExtension(path);

            if (extension != ".pkt" && extension != ".bin" && extension != ".dat")
            {
                extension = await messageBoxService.ShowDialog(new MessageBoxFactory<string>()
                    .SetTitle("Unknown file type")
                    .SetMainInstruction("File doesn't have .pkt, .bin nor .dat extension")
                    .SetContent("Therefore, cannot determine whether to open the file as parsed sniff or unparsed sniff")
                    .WithButton("Open as parsed sniff", ".dat")
                    .WithButton("Open as raw sniff", ".pkt")
                    .Build());
            }
            
            if (extension == ".pkt" || extension == ".bin")
            {
                progress.Report(-1);
                var parsedPath = Path.ChangeExtension(path, null) + "_parsed.dat";
                var parsedTextPath = Path.ChangeExtension(path, null) + "_parsed.txt";
                if (IsFileInUse(parsedTextPath) || IsFileInUse(parsedPath))
                    throw new ParserException("Sniff output file already in use, probably in the middle of parsing, thus can't parse sniff. Are you parsing it in another window already?");    

                var runParser = !File.Exists(parsedPath) || (withText && !File.Exists(parsedTextPath));
                
                if (!runParser)
                {
                    var versionType = await GetVersionAndDump(parsedPath);
                    runParser = !versionType.HasValue ||
                                     !ParserVersionCompatible(versionType.Value.Version) ||
                                     (versionType.Value.Format == DumpFormatType.UniversalProto && withText) ||
                                     (versionType.Value.Format == DumpFormatType.UniversalProtoWithSeparateText && !File.Exists(parsedTextPath));
                }
                
                if (runParser)
                {
                    await packetParserService.RunParser(path, viewerSettings.Settings.Parser, withText ? DumpFormatType.UniversalProtoWithSeparateText : DumpFormatType.UniversalProto, customVersion, token, progress);
                    if (token.IsCancellationRequested)
                    {
                        File.Delete(parsedPath);
                        File.Delete(parsedTextPath);
                    }
                }
                path = parsedPath;
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    throw new ParserException("Parser didn't generate the output file. Aborting. Open debug console (Help->Open Debug Console) and report the issue on github.");
                }

                var versionType2 = await GetVersionAndDump(path);
                if (!versionType2.HasValue)
                {
                    throw new ParserException("Parser didn't generate the output file. Aborting. Open debug console (Help->Open Debug Console) and report the issue on github.");
                }

                if (!ParserVersionCompatible(versionType2.Value.Version))
                {
                    throw new ParserException(
                        $"Your parser generated an output file in a different format ({versionType2.Value.Item1}) than expected (>= {StructureVersion.MinProtobufStructureVersion} and <= {StructureVersion.MaxProtobufStructureVersion}). Aborting. If you downloaded the parser from the internet, make sure you have the latest version.");
                }
            }
            else if (extension == ".dat")
            {
                var parsedTextPath = Path.ChangeExtension(path, "txt");
                var versionType = await GetVersionAndDump(path); 
                bool runParser = !versionType.HasValue ||
                                 !ParserVersionCompatible(versionType.Value.Version) ||
                                 (versionType.Value.Format == DumpFormatType.UniversalProto && withText) ||
                                 (versionType.Value.Format == DumpFormatType.UniversalProtoWithSeparateText && !File.Exists(parsedTextPath));
                
                if (runParser)
                {
                    throw new ParserException(
                        "You have opened _parsed.dat file generated by an older version of the editor.\nIt is not compatible. Please reparse the sniff by reopening the .pkt file");
                }
            }

            if (token.IsCancellationRequested)
                return default;

            if (!File.Exists(path))
            {
                throw new ParserException(
                    "For some reason parser didn't generate the output file. Aborting. If it repeats, report bug on github.");
            }
            
            progress.Report(-1);
            var packets = await Task.Run(() =>
            {
                using var @ref = allocator.Increment();
                unsafe
                {
                    var input = File.ReadAllBytes(path);
                    Packets packets = new Packets();
                    fixed (byte* ptr = input)
                    {
                        var protoReader = new ProtoReader(ptr, input.Length);
                        packets.Read(ref protoReader, ref @ref.Array);
                    }
                    return packets;
                }
            }, token).ConfigureAwait(true);
            progress.Report(1);

            if (token.IsCancellationRequested)
                return default;

            return packets;
        }

        private bool ParserVersionCompatible(ulong version)
        {
            return version >= StructureVersion.MinProtobufStructureVersion &&
                   version <= StructureVersion.MaxProtobufStructureVersion;
        }

        private bool IsFileInUse(string path)
        {
            if (!File.Exists(path))
                return false;
            
            try
            {
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None).Dispose();
            }
            catch (Exception)
            {
                return true;
            }

            return false;
        }

        public unsafe Task<(ulong Version, DumpFormatType Format)?> GetVersionAndDump(string path)
        {
            using var input = File.OpenRead(path);
            var firstBytes = new byte[100]; // enough for the header
            input.Read(firstBytes);

            ulong? version = 0;
            DumpFormatType? formatType = 0;

            fixed (byte* ptr = firstBytes)
            {
                var protoReader = new ProtoReader(ptr, firstBytes.Length);
                while (protoReader.Next())
                {
                    if (protoReader.Tag == PacketsVersion.FieldIds.Version)
                        version = protoReader.ReadVarInt();
                    else if (protoReader.Tag == PacketsVersion.FieldIds.DumpType)
                        formatType = (DumpFormatType)protoReader.ReadVarInt();
                    else if (protoReader.Tag == PacketsVersion.FieldIds.GameVersion)
                        protoReader.ReadVarInt();
                    else
                        break;
                }
            }

            if (!version.HasValue || !formatType.HasValue)
                return Task.FromResult<(ulong, DumpFormatType)?>(null);

            return Task.FromResult<(ulong, DumpFormatType)?>((version.Value, formatType.Value));
        }
    }
}