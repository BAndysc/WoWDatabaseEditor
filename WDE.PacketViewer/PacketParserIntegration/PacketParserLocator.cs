using System.IO;
using System.Linq;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.PacketParserIntegration
{
    [AutoRegister]
    [SingleInstance]
    public class PacketParserLocator : IPacketParserLocator
    {
        private bool located = false;
        private string? localization;
        
        public string? GetPacketParserPath()
        {
            if (!located)
                localization = FindParser();

            return localization;
        }

        private string? FindParser()
        {
            located = true;
            return new[]
                {
                    @"WoWPacketParser\WowPacketParser.exe",
                    @"WoWPacketParser/WowPacketParser.dll",
                    @"WowPacketParser.exe",
                    @"WowPacketParser",
                    @"WowPacketParser.dll",
                    @"..\..\..\WowPacketParser\WowPacketParser\bin\Debug\WowPacketParser.exe",
                    @"../../../WowPacketParser/WowPacketParser/bin/Debug/WowPacketParser.dll",
                    @"..\..\..\WowPacketParser\WowPacketParser\bin\Release\WowPacketParser.exe",
                    @"../../../WowPacketParser/WowPacketParser/bin/Release/WowPacketParser.dll"
                }
                .Select(Path.GetFullPath)
                .FirstOrDefault(File.Exists);
        }
    }
}