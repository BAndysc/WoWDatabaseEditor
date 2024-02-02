using System;
using System.Threading.Tasks;
using EnvDTE;
using WDE.Common.Debugging;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

internal class MockVisualStudioEnv : IDTE
{
    private readonly string solutionPath;

    public MockVisualStudioEnv(string solutionPath)
    {
        this.solutionPath = solutionPath;
    }

    public async Task ConnectAsync()
    {
        // no op
    }

    public bool SkipSolutionPathValidation => true;
    public Task<string?> GetSolutionFullPath() => Task.FromResult<string?>(solutionPath);
    public Task<string> GetIdeName() => Task.FromResult("Visual Studio 2024 Community");
    public async Task DebugUnpause() { }
    public async Task DebugPause() { }
    public async Task ActivateWindow() { }
    public async Task GoToFile(string fileName, int line, bool activate) { }
    public async Task<string?> GetLine(string fileName, int lineNumber) => null;

    public event EventHandler<dbgEventReason>? RunModeEntered;
    public event EventHandler<IdeBreakpointHitEventArgs>? BreakModeEntered;
    public event EventHandler<dbgEventReason>? DebuggingEnded;

    public async Task<dbgDebugMode> GetDebugModeAsync()
    {
        await Task.Delay(100);
        return dbgDebugMode.dbgDesignMode;
    }

    public async ValueTask DisposeAsync() { }
}