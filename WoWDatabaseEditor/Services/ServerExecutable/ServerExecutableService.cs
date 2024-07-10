using System;
using System.IO;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Services.Processes;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.ServerExecutable;

[AutoRegister]
[SingleInstance]
public partial class ServerExecutableService : ObservableBase, IServerExecutableService
{
    private readonly IProcessService processService;
    private readonly Lazy<IMessageBoxService> messageBoxService;
    private readonly IServerExecutableConfiguration configuration;
    private readonly Lazy<IStatusBar> statusBar;
    [Notify] private bool isWorldServerRunning;
    [Notify] private bool isAuthServerRunning;
    public IAsyncCommand ToggleWorldServer { get; }
    public IAsyncCommand ToggleAuthServer { get; }

    private IProcess? worldProcess;
    private IProcess? authProcess;
    
    public ServerExecutableService(IProcessService processService,
        Lazy<IMessageBoxService> messageBoxService,
        IServerExecutableConfiguration configuration,
        Lazy<IStatusBar> statusBar)
    {
        this.processService = processService;
        this.messageBoxService = messageBoxService;
        this.configuration = configuration;
        this.statusBar = statusBar;
        ToggleWorldServer = new AsyncCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(configuration.WorldServerPath) ||
                !File.Exists(configuration.WorldServerPath))
            {
                await ShowConfigureDialog();
                return;
            }
            
            if (worldProcess != null && worldProcess.IsRunning)
            {
                worldProcess.Kill();
                IsWorldServerRunning = false;
                worldProcess = null;
            }
            else
            {
                worldProcess = processService.RunAndForget(
                    configuration.WorldServerPath,
                    Array.Empty<string>(), Directory.GetParent(configuration.WorldServerPath)?.FullName, true);
                worldProcess.OnExit += WorldProcessOnOnExit;
                IsWorldServerRunning = true;
            }
        });
        
        ToggleAuthServer = new AsyncCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(configuration.AuthServerPath) ||
                !File.Exists(configuration.AuthServerPath))
            {
                await ShowConfigureDialog();
                return;
            }

            if (authProcess != null && authProcess.IsRunning)
            {
                authProcess.Kill();
                IsAuthServerRunning = false;
                authProcess = null;
            }
            else
            {
                authProcess = processService.RunAndForget(
                    configuration.AuthServerPath,
                    Array.Empty<string>(), Directory.GetParent(configuration.AuthServerPath)?.FullName, true);
                authProcess.OnExit += AuthProcessOnOnExit;
                IsAuthServerRunning = true;
            }
        });
    }

    private Task ShowConfigureDialog()
    {
        return messageBoxService.Value.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Configuration error")
            .SetMainInstruction("Setup server path first")
            .SetContent("In order to use quick executable start, configure the server paths in the settings")
            .WithOkButton(true)
            .Build());
    }

    private void AuthProcessOnOnExit(int code)
    {
        if (authProcess != null)
            authProcess.OnExit -= AuthProcessOnOnExit;
        if (code != 0)
            statusBar.Value.PublishNotification(new PlainNotification(NotificationType.Warning, "Auth server exited with code " + code));
        IsAuthServerRunning = false;
        authProcess = null;
    }
    
    private void WorldProcessOnOnExit(int code)
    {
        if (worldProcess != null)
            worldProcess.OnExit -= WorldProcessOnOnExit;
        if (code != 0)
            statusBar.Value.PublishNotification(new PlainNotification(NotificationType.Warning, "World server exited with code " + code));
        IsWorldServerRunning = false;
        worldProcess = null;
    }
}