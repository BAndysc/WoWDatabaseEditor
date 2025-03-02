using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;
using WDE.PacketViewer.Services;

namespace WDE.PacketViewer.PacketParserIntegration
{
    [AutoRegister]
    [SingleInstance]
    public class PacketParserService : IPacketParserService
    {
        private readonly IPacketParserLocator locator;
        private readonly IProcessService processService;
        private readonly IUserSettings userSettings;
        private readonly IMessageBoxService messageBoxService;
        private bool acceptedLicence;

        delegate string ParserConfigurationGetterType(ref ParserConfiguration data);
        
        private Dictionary<string, (string key, string defaults, ParserConfigurationGetterType getter)> propertiesToConfig = new();

        public PacketParserService(IPacketParserLocator locator,
            IProcessService processService,
            IUserSettings userSettings,
            IMessageBoxService messageBoxService,
            IApplicationReleaseConfiguration applicationReleaseConfiguration)
        {
            this.locator = locator;
            this.processService = processService;
            this.userSettings = userSettings;
            this.messageBoxService = messageBoxService;
            acceptedLicence = userSettings.Get<Data>().AcceptedLicense || applicationReleaseConfiguration.GetBool("PARSER_LICENCE") is true;

            foreach (var prop in typeof(ParserConfiguration).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attribute = prop.GetCustomAttribute<ParserConfigEntryAttribute>();
                if (attribute == null)
                    continue;

                var getter = (ParserConfigurationGetterType)Delegate.CreateDelegate(typeof(ParserConfigurationGetterType), null, prop.GetGetMethod()!);
                propertiesToConfig[prop.Name] = (attribute.SettingName, attribute.DefaultValue, getter);
            }
        }

        private string GetPacketParserPathOrThrow()
        {
            var path = locator.GetPacketParserPath();
            if (path == null)
                throw new ParserNotFoundException();
            return path;
        }

        private async Task AcceptLicenseOrThrow()
        {
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
        }

        private List<string> GetConfigurationString(ParserConfiguration config, DumpFormatType dumpType, int? customVersion)
        {
            var configuration = propertiesToConfig
                .Values
                .Select(tuple => (tuple.getter(ref config), tuple.defaults, tuple.key))
                .Where(tuple => tuple.defaults != tuple.Item1)
                .SelectMany(tuple => new[] { $"--{tuple.key}", tuple.Item1 })
                .ToList();

            if (customVersion.HasValue)
            {
                configuration.Add("--ClientBuild");
                configuration.Add(customVersion.Value.ToString());
            }

            configuration.Add("--DumpFormat");
            configuration.Add(((int) dumpType).ToString());
            return configuration;
        }

        private class LivePacketParser : ILivePacketParser
        {
            private BinaryWriter writer = null!;

            public void SendRawPacket(ParserRawPacket packet)
            {
                writer.Write(Encoding.ASCII.GetBytes(packet.IsSmsg ? "SMSG" : "CMSG"));
                writer.Write((uint)packet.SessionId);
                writer.Write((ulong)packet.TickCount);
                writer.Write((uint)0);
                writer.Write((uint)packet.Data.Length + 4);
                writer.Write((uint)packet.Opcode);
                writer.Write(packet.Data);
            }

            public event Action<string>? OnPacketParsed;

            public void SetInputStream(Stream stream)
            {
                this.writer = new BinaryWriter(stream);
            }

            public void ReceivedParsedPacket(string packet)
            {
                OnPacketParsed?.Invoke(packet);
            }
        }

        public async Task<ILivePacketParser> RunStreamParser(ParserConfiguration config, DumpFormatType dumpType, CancellationToken token)
        {
            var path = GetPacketParserPathOrThrow();
            await AcceptLicenseOrThrow();
            var args = GetConfigurationString(config, dumpType, null);
            var executable = path;
            args.Add("--stdin");
            args.Add("true");
            args.Add("--stdout");
            args.Add("true");
            if (Path.GetExtension(executable) == ".dll")
            {
                executable = "dotnet";
                args.Insert(0, path);
            }

            var parser = new LivePacketParser();
            var task = processService.Run(token, executable, args, null, onOut =>
            {
                if (!onOut.StartsWith("PKT"))
                    return;

                var base64 = onOut.Substring(3);
                parser.ReceivedParsedPacket(base64);
            }, null, true, out var input);

            parser.SetInputStream(input.BaseStream);

            return parser;
        }
        
        public async Task RunParser(string input, ParserConfiguration config, DumpFormatType dumpType, int? customVersion, CancellationToken token, IProgress<float> progress)
        {
            var path = GetPacketParserPathOrThrow();
            await AcceptLicenseOrThrow();

            var args = GetConfigurationString(config, dumpType, customVersion);
            args.Add(input);

            var executable = path;
            if (Path.GetExtension(executable) == ".dll")
            {
                executable = "dotnet";
                args.Insert(0, path);
            }
            LOG.LogInformation("Running {@executable} {@args}", executable, string.Join(" ", args));
            var processStartInfo = new ProcessStartInfo(executable, args);
            processStartInfo.WorkingDirectory = Path.GetDirectoryName(path)!;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = processStartInfo;
            process.OutputDataReceived += (sender, data) =>
            {
                if (float.TryParse(data.Data, out var p))
                    progress.Report(p);
            };
            process.ErrorDataReceived += (sender, data) =>
            {
                if (data.Data != null)
                    LOG.LogWarning(data.Data);
            };
            
            if (!process.Start())
                throw new CouldNotRunParserException();
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try
            {
                await process.WaitForExitAsync(token);
            }
            catch (TaskCanceledException)
            {
                process.Kill();
            }
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
