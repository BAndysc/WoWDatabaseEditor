using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Services.IdGenerator;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.IdGeneratorService;

[AutoRegister(Platforms.Desktop)]
public class IdGeneratorConfigurationViewModel : ObservableBase, IConfigurable
{
    private readonly IIdGeneratorService idGeneratorService;

    public IdGeneratorConfigurationViewModel(IIdGeneratorService idGeneratorService)
    {
        this.idGeneratorService = idGeneratorService;

        IdTypes = idGeneratorService.KnownIdTypes
            .Select(type => new IdTypeConfigViewModel(type,
                idGeneratorService.GetSources(type),
                idGeneratorService.GetActiveSource(type)))
            .ToList();

        foreach (var idType in IdTypes)
            idType.PropertyChanged += (_, _) => RaisePropertyChanged(nameof(IsModified));

        Save = new DelegateCommand(() =>
        {
            foreach (var idType in IdTypes)
                idType.Apply(idGeneratorService);
            RaisePropertyChanged(nameof(IsModified));
        });
    }

    public IReadOnlyList<IdTypeConfigViewModel> IdTypes { get; }

    public ICommand Save { get; }
    public ImageUri Icon { get; } = new ImageUri("Icons/document_id_big.png");
    public string Name => "Id generation";
    public string? ShortDescription => "Pick how each kind of id (spawn guids, condition entries, ...) is generated and configure the chosen source";
    public bool IsModified => IdTypes.Any(x => x.IsModified);
    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Advanced;
}

public partial class IdTypeConfigViewModel : ObservableBase
{
    private readonly Type idType;
    private readonly Dictionary<IIdSource, IIdSourceConfiguration?> configurations = new();
    private IIdSource? savedSource;

    public IdTypeConfigViewModel(Type idType, IReadOnlyList<IIdSource> sources, IIdSource? activeSource)
    {
        this.idType = idType;
        Sources = sources;
        savedSource = activeSource;
        selectedSource = activeSource;
    }

    public string Name => IdTypeNames.Of(idType);

    public IReadOnlyList<IIdSource> Sources { get; }

    [Notify] [AlsoNotify(nameof(SelectedSourceNeedsSetup), nameof(SelectedSourceConfiguration), nameof(IsModified))]
    private IIdSource? selectedSource;

    public bool SelectedSourceNeedsSetup => selectedSource is { IsConfigured: false };

    /// <summary>The selected source's embedded settings panel, null when it has none.
    /// Cached per source, so switching back and forth keeps unsaved edits.</summary>
    public IIdSourceConfiguration? SelectedSourceConfiguration
    {
        get
        {
            if (selectedSource == null)
                return null;
            if (!configurations.TryGetValue(selectedSource, out var configuration))
            {
                configurations[selectedSource] = configuration = selectedSource.CreateConfiguration();
                if (configuration != null)
                    configuration.PropertyChanged += (_, _) => RaisePropertyChanged(nameof(IsModified));
            }
            return configuration;
        }
    }

    public bool IsModified => selectedSource != savedSource || SelectedSourceConfiguration is { IsModified: true };

    public void Apply(IIdGeneratorService service)
    {
        if (selectedSource != null && selectedSource != savedSource)
        {
            service.SetActiveSource(idType, selectedSource);
            savedSource = selectedSource;
        }
        if (SelectedSourceConfiguration is { IsModified: true } configuration)
            configuration.Apply();
        RaisePropertyChanged(nameof(SelectedSourceNeedsSetup));
    }
}
