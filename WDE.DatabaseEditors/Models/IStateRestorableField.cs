using WDE.DatabaseEditors.Solution;

namespace WDE.DatabaseEditors.Models
{
    public interface IStateRestorableField
    {
        void RestoreLoadedFieldState(DatabaseSolutionItemModifiedField fieldData);
        object? GetValueForPersistence();
        object GetOriginalValueForPersistence();
    }
}