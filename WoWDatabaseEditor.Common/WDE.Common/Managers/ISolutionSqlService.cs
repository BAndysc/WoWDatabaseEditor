using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface ISolutionSqlService
    {
        void OpenDocumentWithSqlFor(ISolutionItem solutionItem);
    }
}