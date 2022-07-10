using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.ServerExecutable;

[SingleInstance]
[AutoRegister]
public class ServerExecutableConfiguration : IServerExecutableConfiguration
{
    private readonly IUserSettings userSettings;
    public string? WorldServerPath { get; set; }
    public string? AuthServerPath { get; set; }

    public ServerExecutableConfiguration(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        var data = userSettings.Get<Data>();
        WorldServerPath = data.WorldServerPath;
        AuthServerPath = data.AuthServerPath;
    }
    
    public void Update(string? worldServerPath, string? authServerPath)
    {
        worldServerPath = string.IsNullOrWhiteSpace(worldServerPath) ? null : worldServerPath;
        authServerPath = string.IsNullOrWhiteSpace(authServerPath) ? null : authServerPath;
        WorldServerPath = worldServerPath;
        AuthServerPath = authServerPath;
        userSettings.Update(new Data(){WorldServerPath = worldServerPath, AuthServerPath = authServerPath});
    }

    private struct Data : ISettings
    {
        public string? WorldServerPath { get; set; }
        public string? AuthServerPath { get; set; }
    }
}