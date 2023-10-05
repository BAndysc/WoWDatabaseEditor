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
#if DEBUG
                    @"..\..\..\WoWPacketParser\WowPacketParser\bin\Debug\WowPacketParser.exe",
                    @"../../../WoWPacketParser/WowPacketParser/bin/Debug/WowPacketParser.dll",
#endif
                    @"..\..\..\WoWPacketParser\WowPacketParser\bin\Release\WowPacketParser.exe",
                    @"../../../WoWPacketParser/WowPacketParser/bin/Release/WowPacketParser.dll",
                    
                    @"WoWPacketParser\WowPacketParser.exe",
                    @"WoWPacketParser/WowPacketParser.dll",
                    @"WowPacketParser.exe",
                    @"WowPacketParser",
                    @"WowPacketParser.dll",
                    
                    @"parser/WowPacketParser.exe",
                    @"parser/WowPacketParser",
                    @"parser/WowPacketParser.dll"
                }
                .Select(Path.GetFullPath)
                .FirstOrDefault(File.Exists);
        }
    }
}