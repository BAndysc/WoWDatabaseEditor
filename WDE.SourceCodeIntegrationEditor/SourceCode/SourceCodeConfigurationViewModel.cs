using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.SourceCodeIntegrationEditor.SourceCode;

[AutoRegister]
internal partial class SourceCodeConfigurationViewModel : ObservableBase, IConfigurable, IFirstTimeWizardConfigurable
{
    [Notify] private string? selectedPath;

    public ObservableCollection<string> Paths { get; } = new();

    public IAsyncCommand AddDirectoryCommand { get; }
    public DelegateCommand DeleteSelectedCommand { get; }

    public SourceCodeConfigurationViewModel(ISourceCodeConfiguration sourceCodeConfiguration,
        Lazy<IWindowManager> windowManager)
    {
        Paths.AddRange(sourceCodeConfiguration.SourceCodePaths);

        DeleteSelectedCommand = new DelegateCommand(() =>
        {
            if (selectedPath == null)
                return;

            var indexOfSelected = Paths.IndexOf(selectedPath);
            if (indexOfSelected != -1)
            {
                Paths.RemoveAt(indexOfSelected);
                IsModified = true;
                indexOfSelected = Math.Clamp(indexOfSelected - 1, 0, Paths.Count - 1);
                if (Paths.Count > 0)
                    SelectedPath = Paths[indexOfSelected];
                else
                    SelectedPath = null;
            }
        }, () => selectedPath != null).ObservesProperty<string?>(() => SelectedPath);

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
            IsModified = false;
        });
    }

    public ICommand Save { get; }
    public string Name => "Server source code";
    public string? ShortDescription => "If you provide the path to the server source code, Find Anywhere will use it to search in the source code.";
    [Notify] private bool isModified;
    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
}