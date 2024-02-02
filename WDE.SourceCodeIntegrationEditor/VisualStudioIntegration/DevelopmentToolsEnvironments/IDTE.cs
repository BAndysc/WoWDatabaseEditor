using System;
using System.Threading.Tasks;
using EnvDTE;
using WDE.Common.Debugging;

namespace WDE.SourceCodeIntegrationEditor.VisualStudioIntegration.DevelopmentToolsEnvironments;

// ReSharper disable once InconsistentNaming
internal interface IDTE : System.IAsyncDisposable
{
    Task ConnectAsync();

    bool SkipSolutionPathValidation { get; }

    Task<string?> GetSolutionFullPath();
    Task<string> GetIdeName();
    Task DebugUnpause();
    Task DebugPause();
    Task ActivateWindow();
    Task GoToFile(string fileName, int line, bool activate);
    Task<string?> GetLine(string fileName, int lineNumber);

    /// <summary>
    /// Invoked when the IDE enters run mode (i.e. after the app is started/continued)
    /// </summary>
    event EventHandler<dbgEventReason> RunModeEntered;
    event EventHandler<IdeBreakpointHitEventArgs> BreakModeEntered;
    event EventHandler<dbgEventReason> DebuggingEnded;

    Task<dbgDebugMode> GetDebugModeAsync();
}