namespace WDE.DatabaseEditors.Data
{
    public interface IDbTableFieldNameSwapHandler
    {
        void OnFieldValueChanged(long newValue, string fieldName);
    }
}