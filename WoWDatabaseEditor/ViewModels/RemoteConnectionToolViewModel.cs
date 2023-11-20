using System;
using System.Diagnostics;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.ViewModels;

[AutoRegister]
[SingleInstance]
public partial class RemoteConnectionToolViewModel : ObservableBase, ITool
{
    private readonly IRemoteConnectorService remoteConnector;
    private readonly IMainThread mainThread;
    public string Title => "Remote connection console";
    public string UniqueId => "remote_connection_console";
    private bool visibility = false;

    public ICommand ClearConsole {get;}

    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Bottom;
    public bool OpenOnStart => false;

    public INativeTextDocument Text { get; }
    public bool Visibility
    {
        get => visibility; 
        set
        {
            if (value == visibility)
                return;
            if (value)
            {
                remoteConnector.OnLog += OnTrace;
            }
            else
            {
                remoteConnector.OnLog -= OnTrace;
            }
            SetProperty(ref visibility, value);
        }
    }

    private void OnTrace(string message, TimeSpan time, RemoteCommandType type)
    {
        mainThread.Dispatch(() =>
        {
            switch (type)
            {
                case RemoteCommandType.Send:
                    Text.Append($"> {message}\n");
                    break;
                case RemoteCommandType.Receive:
                    Text.Append($"{message} (took {time})\n");
                    break;
                case RemoteCommandType.Debug:
                    if (time == default)
                        Text.Append($"[DEBUG] {message}\n");
                    else
                        Text.Append($"[DEBUG] {message} (took {time})\n");
                    break;
                default:
                    Text.Append($"[{type}] {message} (took {time})\n");
                    break;
            }
            ScrollToEnd.Invoke();
        });
    }

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
    
    public RemoteConnectionToolViewModel(IRemoteConnectorService remoteConnector, IMainThread mainThread, INativeTextDocument document)
    {
        this.remoteConnector = remoteConnector;
        this.mainThread = mainThread;
        this.Text = document;

        ClearConsole = new DelegateCommand(() =>
        {
            Clear.Invoke();
            ScrollToEnd.Invoke();      
        });
    }

    public event Action ScrollToEnd = delegate {};
    public event Action Clear = delegate {};
}