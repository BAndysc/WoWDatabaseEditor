using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Data
{
    public interface IDbTableFieldNameSwapHandler
    {
        void SwapFieldName(IDbTableField field);
    }
}