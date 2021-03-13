using WDE.DatabaseEditors.Solution;

namespace WDE.DatabaseEditors.Models
{
    public interface IStateRestorableField
    {
        void RestoreLoadedFieldState(DbTableSolutionItemModifiedField fieldData);
        object? GetValueForPersistence();
    }
}