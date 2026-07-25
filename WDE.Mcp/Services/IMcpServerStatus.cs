using System.ComponentModel;
using WDE.Module.Attributes;

namespace WDE.Mcp.Services;

[UniqueProvider]
public interface IMcpServerStatus : INotifyPropertyChanged
{
    bool IsRunning { get; }
    string? Url { get; }
    string? LastError { get; }
    int ToolCount { get; }
}
