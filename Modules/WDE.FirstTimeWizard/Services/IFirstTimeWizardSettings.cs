using WDE.Module.Attributes;

namespace WDE.FirstTimeWizard.Services;

[UniqueProvider]
public interface IFirstTimeWizardSettings
{
    FirstTimeWizardState State { get; set; }
}