using WDE.Common.CoreVersion;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.FirstTimeWizard.Services;

[SingleInstance]
[AutoRegister]
public class FirstTimeWizardSettings : IFirstTimeWizardSettings
{
    private readonly IUserSettings userSettings;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private FirstTimeWizardState state;

    public FirstTimeWizardSettings(IUserSettings userSettings, ICurrentCoreVersion currentCoreVersion)
    {
        this.userSettings = userSettings;
        this.currentCoreVersion = currentCoreVersion;
        state = userSettings.Get<Data>().State;
        if (!currentCoreVersion.IsSpecified && state == FirstTimeWizardState.HasCoreVersion)
            state = FirstTimeWizardState.None;
    }

    private struct Data : ISettings
    {
        public FirstTimeWizardState State { get; set; }
    }

    public FirstTimeWizardState State
    {
        get => state;
        set
        {
            state = value;
            userSettings.Update(new Data {State = value});
        }
    }
}
