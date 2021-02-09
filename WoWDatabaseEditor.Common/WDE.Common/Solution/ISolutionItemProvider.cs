using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [NonUniqueProvider]
    public interface ISolutionItemProvider
    {
        string GetName();
        ImageUri GetImage();
        string GetDescription();
        
        bool IsCompatibleWithCore(ICoreVersion core);

        Task<ISolutionItem> CreateSolutionItem();
    }
}