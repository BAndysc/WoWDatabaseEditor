using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.QuestChainEditor.GraphLayouting.ViewModels;

[AutoRegister]
[SingleInstance]
public partial class GraphLayoutSettingsViewModel : ObservableBase
{
    private readonly IUserSettings userSettings;
    private readonly IMessageBoxService messageBoxService;
    private Data savedData;

    public ObservableCollection<LayoutAlgorithmSettingsViewModel> Algorithms { get; } = new()
    {
        new MAGLLayoutAlgorithmSettingsViewModel(),
        new EfficientSugiyamaLayoutAlgorithmSettingsViewModel(),
        new SugiyamaLayoutAlgorithmSettingsViewModel(),
        new SimpleTreeLayoutAlgorithmSettingsViewModel(),
        new CompoundFDPLayoutAlgorithmSettingsViewModel(),
        new ISOMLayoutAlgorithmSettingsViewModel(),
        new FRLayoutAlgorithmSettingsViewModel(),
        new KKLayoutAlgorithmSettingsViewModel(),
        new LinLogLayoutAlgorithmSettingsViewModel(),
        new BalloonTreeLayoutAlgorithmSettingsViewModel()
    };

    private LayoutAlgorithmSettingsViewModel? currentAlgorithm;
    public LayoutAlgorithmSettingsViewModel? CurrentAlgorithm
    {
        get => currentAlgorithm;
        set
        {
            if (currentAlgorithm != null)
                currentAlgorithm.SettingsChanged -= OnSettingsChanged;

            SetProperty(ref currentAlgorithm, value);

            if (currentAlgorithm != null)
                currentAlgorithm.SettingsChanged += OnSettingsChanged;

            OnSettingsChanged(null); // pass null to update only current algorithm name
        }
    }

    private void OnSettingsChanged(LayoutAlgorithmSettingsViewModel? viewModel)
    {
        SettingsChanged?.Invoke();
        savedData.currentAlgorithm = CurrentAlgorithm?.Name;
        if (viewModel != null)
            savedData.settings[viewModel.Name] = viewModel.SaveAsJson();
        userSettings.Update(savedData);
    }

    public GraphLayoutSettingsViewModel(IUserSettings userSettings,
        IMessageBoxService messageBoxService)
    {
        this.userSettings = userSettings;
        this.messageBoxService = messageBoxService;
        savedData = userSettings.Get<Data>(new Data())!;
        CurrentAlgorithm = Algorithms.FirstOrDefault(a => a.Name == savedData.currentAlgorithm) ?? Algorithms[0];
        savedData.settings ??= new();
        Load().ListenErrors();
    }

    private async Task Load()
    {
        bool? resetSettings = null;
        foreach (var algorithm in Algorithms)
        {
            if (savedData.settings.TryGetValue(algorithm.Name, out var saved))
            {
                var savedVersion = saved["__version"]?.Value<int>() ?? 0;
                if (algorithm.Version > savedVersion)
                {
                    if (!resetSettings.HasValue)
                    {
                        resetSettings = await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                            .SetTitle("Reset settings")
                            .SetMainInstruction("Default graph layout settings have been changed")
                            .SetContent(
                                "The default graph layout setting have changed since you last saved them. Do you want to use the default settings or your saved settings?")
                            .SetIcon(MessageBoxIcon.Warning)
                            .WithButton("Reset to default", true)
                            .WithButton("Use my settings", false)
                            .Build());
                    }

                    if (resetSettings.Value)
                    {
                        savedData.settings[algorithm.Name] = algorithm.SaveAsJson();
                        continue;
                    }
                }

                algorithm.Load(saved);
            }
        }
    }

    public event System.Action? SettingsChanged;

    public class Data : ISettings
    {
        public Dictionary<string, JObject> settings = new();
        public string? currentAlgorithm { get; set; }
    }
}