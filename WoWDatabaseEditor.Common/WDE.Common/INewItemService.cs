using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface INewItemService
    {
        Task<ISolutionItem?> GetNewSolutionItem(bool showFolders = true);
    }
}