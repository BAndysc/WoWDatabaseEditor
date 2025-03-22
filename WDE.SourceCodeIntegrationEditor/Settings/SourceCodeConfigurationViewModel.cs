using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.SourceCodeIntegrationEditor.Settings;

[AutoRegister]
internal partial class SourceCodeConfigurationViewModel : ObservableBase, IConfigurable, IFirstTimeWizardConfigurable
{
    public ObservableCollection<string> Paths { get; } = new();

    public IAsyncCommand AddDirectoryCommand { get; }
    public DelegateCommand<string> DeleteCommand { get; }

    [Notify] private bool enableVisualStudioIntegration;
    [Notify] private bool enableRemoteVisualStudioConnection;
    [Notify] private string remoteVisualStudioAddress = "";
    [Notify] private string remoteVisualStudioKey = "";

    public bool SupportsRemoteVisualStudio { get; }

    public SourceCodeConfigurationViewModel(ISourceCodeConfiguration sourceCodeConfiguration,
        Lazy<IWindowManager> windowManager)
    {
        Paths.AddRange(sourceCodeConfiguration.SourceCodePaths);
        enableVisualStudioIntegration = sourceCodeConfiguration.EnableVisualStudioIntegration;
        enableRemoteVisualStudioConnection = sourceCodeConfiguration.EnableRemoteVisualStudioConnection;
        remoteVisualStudioAddress = sourceCodeConfiguration.RemoteVisualStudioAddress;
        remoteVisualStudioKey = sourceCodeConfiguration.RemoteVisualStudioKey;

        SupportsRemoteVisualStudio = !OperatingSystem.IsWindows();

        DeleteCommand = new DelegateCommand<string>(s =>
        {
            var indexOfSelected = Paths.IndexOf(s);
            if (indexOfSelected != -1)
            {
                Paths.RemoveAt(indexOfSelected);
                IsModified = true;
            }
        });

        AddDirectoryCommand = new AsyncCommand(async () =>
        {
            var result = await windowManager.Value.ShowFolderPickerDialog(Environment.CurrentDirectory);
            if (result != null)
            {
                Paths.Add(result);
                IsModified = true;
            }
        });

        Save = new DelegateCommand(() =>
        {
            sourceCodeConfiguration.SourceCodePaths = Paths;
            sourceCodeConfiguration.EnableVisualStudioIntegration = enableVisualStudioIntegration;
            sourceCodeConfiguration.EnableRemoteVisualStudioConnection = enableRemoteVisualStudioConnection;
            sourceCodeConfiguration.RemoteVisualStudioAddress = remoteVisualStudioAddress;
            sourceCodeConfiguration.RemoteVisualStudioKey = remoteVisualStudioKey;
            sourceCodeConfiguration.Save();
            IsModified = false;
        });

        On(() => EnableVisualStudioIntegration, _ => IsModified = true);
        On(() => EnableRemoteVisualStudioConnection, _ => IsModified = true);
        On(() => RemoteVisualStudioAddress, _ => IsModified = true);
        On(() => RemoteVisualStudioKey, _ => IsModified = true);
        IsModified = false;
    }

    public ICommand Save { get; }
    public ImageUri Icon { get; } = new ImageUri("Icons/document_code_big.png");
    public string Name => "Server source code";
    public string? ShortDescription => "If you provide the path to the server source code, Find Anywhere will use it to search in the source code.";
    [Notify] private bool isModified;
    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
}