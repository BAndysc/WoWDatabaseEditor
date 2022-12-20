using System;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.FirstTimeWizard.ViewModels;
using WDE.Module.Attributes;

namespace WDE.FirstTimeWizard.Services;

[SingleInstance]
[AutoRegister]
public class FirstTimeWizardService : IFirstTimeWizardService
{
    private readonly IWindowManager windowManager;
    private readonly ICurrentCoreVersion coreVersion;
    private readonly IFirstTimeWizardSettings wizardSettings;
    private readonly Func<FirstTimeWizardViewModel> viewModel;

    public FirstTimeWizardService(IWindowManager windowManager,
        ICurrentCoreVersion coreVersion,
        IFirstTimeWizardSettings wizardSettings,
        Func<FirstTimeWizardViewModel> viewModel)
    {
        this.windowManager = windowManager;
        this.coreVersion = coreVersion;
        this.wizardSettings = wizardSettings;
        this.viewModel = viewModel;
    }
    
    public async Task OpenWizard()
    {
        await windowManager.ShowDialog(viewModel());
    }

    public void Run()
    {
        switch (wizardSettings.State)
        {
            case FirstTimeWizardState.None:
                if (!coreVersion.IsSpecified)
                    OpenWizard().ListenErrors();
                break;
            case FirstTimeWizardState.Canceled:
                break;
            case FirstTimeWizardState.HasCoreVersion:
                OpenWizard().ListenErrors();
                break;
            case FirstTimeWizardState.Completed:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}