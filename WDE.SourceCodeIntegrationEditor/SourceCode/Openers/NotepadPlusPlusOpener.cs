using System;
using System.IO;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[AutoRegister]
[SingleInstance]
public class NotepadPlusPlusOpener : IFileOpener
{
    private readonly IProgramFinder programFinder;
    private readonly IProcessService processService;
    public string Name => "Notepad++";
    public int Order => 20;

    public NotepadPlusPlusOpener(IProgramFinder programFinder, IProcessService processService)
    {
        this.programFinder = programFinder;
        this.processService = processService;
    }

    public bool TryOpen(FileInfo path, int lineNumber)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var exe = programFinder.TryLocate("Notepad++/notepad++.exe");
        if (exe == null || !File.Exists(exe))
            return false;

        var process = processService.RunAndForget(exe, new[]{path.FullName, $"-n{lineNumber}"}, null, false);
        return process.IsRunning;
    }
}