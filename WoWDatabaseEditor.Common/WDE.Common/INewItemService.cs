using WDE.Common.Annotations;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [UniqueProvider]
    public interface INewItemService
    {
        [CanBeNull]
        ISolutionItem GetNewSolutionItem();
    }
}