using AsyncAwaitBestPractices.MVVM;

namespace WoWDatabaseEditorCore.Services.ServerExecutable;

public interface IServerExecutableService
{
    IAsyncCommand ToggleWorldServer { get; }
    bool IsWorldServerRunning { get; }
    
    IAsyncCommand ToggleAuthServer { get; }
    bool IsAuthServerRunning { get; }
}