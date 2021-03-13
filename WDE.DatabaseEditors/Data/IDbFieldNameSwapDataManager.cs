namespace WDE.DatabaseEditors.Data
{
    public interface IDbFieldNameSwapDataManager
    {
        void RegisterSwapDefinition(string tableName, string path);
        TableFieldsNameSwapDefinition? GetSwapData(string tableName);
    }
}