using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.ServerExecutable;

[AutoRegister(Platforms.Desktop)]
public partial class ServerExecutableConfigurationPanelViewModel : ObservableBase, IConfigurable
{
    private readonly IWindowManager windowManager;
    private readonly IServerExecutableConfiguration configuration;
    public ICommand Save { get; }
    public string Name => "World server executable";
    public string? ShortDescription => "You can configure your world and auth server paths for easy start/stop button access in the statusbar";
    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Basic;

    public bool IsModified => worldServerPath != configuration.WorldServerPath || authServerPath != configuration.AuthServerPath;
    [Notify] [AlsoNotify(nameof(IsModified))] private string? worldServerPath;
    [Notify] [AlsoNotify(nameof(IsModified))] private string? authServerPath;
    
    public ICommand PickWorldPath { get; }
    public ICommand PickAuthPath { get; }
    
    public ServerExecutableConfigurationPanelViewModel(IWindowManager windowManager, 
        IServerExecutableConfiguration configuration)
    {
        this.windowManager = windowManager;
        this.configuration = configuration;
        worldServerPath = configuration.WorldServerPath;
        authServerPath = configuration.AuthServerPath;
        Save = new DelegateCommand(() =>
        {
            configuration.Update(worldServerPath, authServerPath);
            RaisePropertyChanged(nameof(IsModified));
        });

        string filter = "All files|*";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            filter = "Windows executable|exe|All files|*";
        
        PickWorldPath = new AsyncAutoCommand(async () =>
        {
            var path = await windowManager.ShowOpenFileDialog(filter, File.Exists(WorldServerPath) ? Directory.GetParent(WorldServerPath)?.FullName : null);
            if (path != null && File.Exists(path))
                WorldServerPath = path;
        });
        
        PickAuthPath = new AsyncAutoCommand(async () =>
        {
            var path = await windowManager.ShowOpenFileDialog(filter, File.Exists(AuthServerPath) ? Directory.GetParent(AuthServerPath)?.FullName : null);
            if (path != null && File.Exists(path))
                AuthServerPath = path;
        });
    }
}