using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.ServerExecutable;

[UniqueProvider]
public interface IServerExecutableConfiguration
{
    string? WorldServerPath { get; }
    string? AuthServerPath { get; }
    void Update(string? worldServerPath, string? authServerPath);
}