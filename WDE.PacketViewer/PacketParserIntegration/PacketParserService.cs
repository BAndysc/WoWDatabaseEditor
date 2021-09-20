using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

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

        delegate string ParserConfigurationGetterType(ref ParserConfiguration data);
        
        private Dictionary<string, (string key, string defaults, ParserConfigurationGetterType getter)> propertiesToConfig = new();

        public PacketParserService(IPacketParserLocator locator, 
            IUserSettings userSettings,
            IMessageBoxService messageBoxService,
            IApplicationReleaseConfiguration applicationReleaseConfiguration)
        {
            this.locator = locator;
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
        
        public async Task RunParser(string input, ParserConfiguration config, DumpFormatType dumpType, int? customVersion, CancellationToken token, IProgress<float> progress)
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

            var configuration = string.Join(" ", propertiesToConfig
                .Values
                .Select(tuple => (tuple.getter(ref config), tuple.defaults, tuple.key))
                .Where(tuple => tuple.defaults != tuple.Item1)
                .Select(tuple => $"--{tuple.key} \"{tuple.Item1}\""));

            if (customVersion.HasValue)
                configuration += $" --ClientBuild {customVersion.Value}";
                    
            var args = $"{configuration} --DumpFormat {(int) dumpType} \"{input}\"";

            var executable = path;
            if (Path.GetExtension(executable) == ".dll")
            {
                executable = "dotnet";
                args = path + " " + args;
            }
            Console.WriteLine($"Running {executable} {args}");
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