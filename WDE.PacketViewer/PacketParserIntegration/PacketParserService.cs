using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.PacketParserIntegration
{
    [AutoRegister]
    [SingleInstance]
    public class PacketParserService : IPacketParserService
    {
        private readonly IPacketParserLocator locator;
        private readonly IUserSettings userSettings;
        private readonly IMessageBoxService messageBoxService;
        private bool acceptedLicence;

        public PacketParserService(IPacketParserLocator locator, IUserSettings userSettings, IMessageBoxService messageBoxService)
        {
            this.locator = locator;
            this.userSettings = userSettings;
            this.messageBoxService = messageBoxService;
            acceptedLicence = userSettings.Get<Data>().AcceptedLicense;
        }
        
        public async Task RunParser(string input, DumpFormatType dumpType, IProgress<float> progress)
        {
            var path = locator.GetPacketParserPath();
            if (path == null)
                throw new ParserNotFoundException();

            if (!acceptedLicence)
            {
                if (await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetIcon(MessageBoxIcon.Information)
                    .SetTitle("Terms of use")
                    .SetMainInstruction("Terms of Wow Packet Parser use")
                    .SetContent(
                        "WDE uses TrinityCore's Wow Packet Parser. To use the parser, you have to agree to WPP GPL3 licence.\nhttps://github.com/TrinityCore/WowPacketParser/blob/master/COPYING\n\nDo you accept the terms of use?")
                    .WithYesButton(true)
                    .WithNoButton(false)
                    .Build()))
                {
                    acceptedLicence = true;
                    userSettings.Update(new Data(){AcceptedLicense = true});
                }
                else
                {
                    throw new NoAgreementForParserLicence();
                }
            }
            
            var args = $"--DumpFormat {(int) dumpType} \"{input}\"";

            var executable = path;
            if (Path.GetExtension(executable) == ".dll")
            {
                executable = "dotnet";
                args = path + " " + args;
            }
            
            var processStartInfo = new ProcessStartInfo(executable, args);
            processStartInfo.WorkingDirectory = Path.GetDirectoryName(path)!;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = processStartInfo;
            process.OutputDataReceived += (sender, data) =>
            {
                if (float.TryParse(data.Data, out var p))
                    progress.Report(p);
            };
            
            if (!process.Start())
                throw new CouldNotRunParserException();
            
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();
        }

        public readonly struct Data : ISettings
        {
            public bool AcceptedLicense { get; init; }
        }
    }

    public class ParserNotFoundException : ParserException
    {
        public ParserNotFoundException() : base("Couldn't find parser!")
        {
        }
    }

    public class NoAgreementForParserLicence : ParserException
    {
        public NoAgreementForParserLicence() : base("You cannot run the parser without accepting the terms of use!")
        {
        }
    }
    
    public class CouldNotRunParserException : ParserException
    {
        public CouldNotRunParserException() : base("Can't run parser for unknown reason!")
        {
        }
    }
}