using System;
using System.IO;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;

namespace WDE.SourceCodeIntegrationEditor.SourceCode.Openers;

[AutoRegister]
[SingleInstance]
public class NotepadOpener : IFileOpener
{
    private readonly IProgramFinder programFinder;
    private readonly IProcessService processService;
    public string Name => "notepad.exe";
    public int Order => int.MaxValue;

    public NotepadOpener(IProgramFinder programFinder, IProcessService processService)
    {
        this.programFinder = programFinder;
        this.processService = processService;
    }

    public bool TryOpen(FileInfo path, int lineNumber)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var exe = programFinder.TryLocate("notepad.exe");
        if (exe == null || !File.Exists(exe))
            return false;

        var process = processService.RunAndForget(exe, new[]{path.FullName}, null, false);
        return process.IsRunning;
    }
}