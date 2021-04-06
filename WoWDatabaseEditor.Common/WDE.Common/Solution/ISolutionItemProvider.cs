using System;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common
{
    //@todo: replace getters with properties
    [NonUniqueProvider]
    public interface ISolutionItemProvider
    {
        string GetName();
        ImageUri GetImage();
        string GetDescription();
        string GetGroupName();
        
        bool IsCompatibleWithCore(ICoreVersion core);

        Task<ISolutionItem> CreateSolutionItem();
    }

    public interface INamedSolutionItemProvider : ISolutionItemProvider
    {
        Task<ISolutionItem> CreateSolutionItem(string name);
    }
}