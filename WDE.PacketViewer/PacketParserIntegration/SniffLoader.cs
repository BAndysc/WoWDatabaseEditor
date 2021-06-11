using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WowPacketParser.PacketStructures;
//using WowPacketParser.PacketStructures;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration
{
    [AutoRegister]
    [SingleInstance]
    public class SniffLoader : ISniffLoader
    {
        private readonly IPacketParserService packetParserService;
        private readonly IMessageBoxService messageBoxService;

        public SniffLoader(IPacketParserService packetParserService,
            IMessageBoxService messageBoxService)
        {
            this.packetParserService = packetParserService;
            this.messageBoxService = messageBoxService;
        }
        
        public async Task<Packets> LoadSniff(string path, IProgress<float> progress)
        {
            Packets packets = null!;
            var extension = Path.GetExtension(path);

            if (extension != ".pkt" && extension != ".dat")
            {
                extension = await messageBoxService.ShowDialog(new MessageBoxFactory<string>()
                    .SetTitle("Unknown file type")
                    .SetMainInstruction("File doesn't have .pkt nor .dat extension")
                    .SetContent("Therefore, cannot determine whether to open the file as parsed sniff or unparsed sniff")
                    .WithButton("Open as parsed sniff", ".dat")
                    .WithButton("Open as raw sniff", ".pkt")
                    .Build());
            }
            
            if (extension == ".pkt")
            {
                progress.Report(-1);
                var parsedPath = Path.ChangeExtension(path, null) + "_parsed.dat";
                var parsedTextPath = Path.ChangeExtension(path, null) + "_parsed.txt";
                if (IsFileInUse(parsedTextPath) || IsFileInUse(parsedPath))
                    throw new ParserException("Sniff output file already in use, probably in the middle of parsing, thus can't parse sniff. Are you parsing it in another window already?");    

                bool runParser = !File.Exists(parsedPath) || !await VerifyParsedVersion(parsedPath);

                if (runParser)
                {
                    await packetParserService.RunParser(path, DumpFormatType.UniversalProtoWithText, progress);
                }
                path = parsedPath;
            }
            
            progress.Report(-1);
            await Task.Run(async () =>
            {
                await using var input = File.OpenRead(path);
                packets = Packets.Parser.ParseFrom(input);
            }).ConfigureAwait(true);
            progress.Report(1);

            return packets;
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

        public async Task<bool> VerifyParsedVersion(string path)
        {
            await using var input = File.OpenRead(path);
            try
            {
                var version = PacketsVersion.Parser.ParseFrom(input);
                input.Close();
                return version.Version == StructureVersion.ProtobufStructureVersion;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}