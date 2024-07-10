using System.IO;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[AutoRegister]
[SingleInstance]
public class VisualStudioCodeOpener : IFileOpener
{
    private readonly IProgramFinder programFinder;
    private readonly IProcessService processService;
    public string Name => "Visual Studio Code";
    public int Order => 10;

    public VisualStudioCodeOpener(IProgramFinder programFinder, IProcessService processService)
    {
        this.programFinder = programFinder;
        this.processService = processService;
    }

    public bool TryOpen(FileInfo path, int lineNumber)
    {
        var exe = programFinder.TryLocate("code", "code.exe", "Microsoft VS Code/Code.exe");
        if (exe == null || !File.Exists(exe))
            return false;

        var process = processService.RunAndForget(exe, new[]{"--goto", $"{path.FullName}:{lineNumber}"}, null, false);
        return process.IsRunning;
    }
}