using System.Windows.Media;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [NonUniqueProvider]
    public interface ISolutionItemProvider
    {
        string GetName();
        ImageSource GetImage();
        string GetDescription();
        
        bool IsCompatibleWithCore(ICoreVersion core);

        ISolutionItem CreateSolutionItem();
    }
}