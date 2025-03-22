using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Settings;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WoWDatabaseEditorCore.Services.GenericSettings;

[AutoRegister]
public class GeneralSettingsViewModel : ObservableBase, IGeneralSettings
{
    public GeneralSettingsViewModel(IEnumerable<IGeneralSettingsGroup> groups)
    {
        Groups = groups.Select(x => new GeneralSettingsGroupViewModel(x)).ToList();

        foreach (var group in Groups)
        {
            group.ToObservable(g => g.IsModified)
                .SubscribeAction(_ => RaisePropertyChanged(nameof(IsModified)));
        }

        Save = new DelegateCommand(() =>
        {
            foreach (var group in Groups)
            {
                group.Save();
            }
        });
    }
    
    public IList<GeneralSettingsGroupViewModel> Groups { get; }

    public ICommand Save { get; }
    public ImageUri Icon { get; } = new ImageUri("Icons/document_setting_big.png");
    public string Name => "General settings";
    public string? ShortDescription => null;
    public bool IsModified => Groups.Any(x => x.IsModified);
    public bool IsRestartRequired => false;
    public ConfigurableGroup Group => ConfigurableGroup.Basic;
}

public partial class GeneralSettingsGroupViewModel : ObservableBase
{
    private readonly IGeneralSettingsGroup group;
    public string Name { get; }
    public IReadOnlyList<IGenericSetting> Settings { get; }

    [Notify] private bool isModified;

    public GeneralSettingsGroupViewModel(IGeneralSettingsGroup group)
    {
        this.group = group;
        Name = group.Name;
        Settings = group.Settings;

        foreach (var setting in Settings)
            setting.PropertyChanged += (sender, args) => IsModified = true;
    }

    public void Save()
    {
        group.Save();
        IsModified = false;
    }
}