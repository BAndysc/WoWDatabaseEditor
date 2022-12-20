using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Profiles;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.Profiles;

public partial class ProfileCreateViewModel : ObservableBase, IDialog
{
    public IList<ICoreVersion> CoreVersions { get; }

    [Notify] private ICoreVersion? selectedCoreVersion;
    [Notify] private string profileName = "";
    [Notify] private bool makeDefault;
    [AlsoNotify(nameof(RebasedHue))] [Notify] private double hue;

    public double RebasedHue => hue + 0.7f; // AvaloniaStyles.SystemTheme.BaseHue
    
    public ProfileCreateViewModel(IEnumerable<ICoreVersion> coreVersions,
        IProfileService profileService)
    {
        CoreVersions = coreVersions.ToList();
        Accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
            if (!NoCreate)
            {
                profileService.CreateProfile(profileName, selectedCoreVersion!, makeDefault, hue);
            }
        }, () => !string.IsNullOrWhiteSpace(profileName) && (NoCreate || selectedCoreVersion != null))
            .ObservesProperty(() => ProfileName)
            .ObservesProperty(() => SelectedCoreVersion);
        
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
    }

    public int DesiredWidth => 400;
    public int DesiredHeight => 400;
    public string Title => NoCreate ? "Edit new profile" : "Create new profile";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public bool NoCreate { get; set; }

    public event Action? CloseCancel;
    public event Action? CloseOk;
}