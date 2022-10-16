using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.FirstTimeWizard.Services;

[UniqueProvider]
public interface IFirstTimeWizardService
{
    Task OpenWizard();
    void Run();
}
