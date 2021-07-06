using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common.Documents
{
    [NonUniqueProvider]
    public interface IWizardProvider
    {
        string Name { get; }
        ImageUri Image { get; }
        bool IsCompatibleWithCore(ICoreVersion core);

        Task<IWizard> Create();
    }
}