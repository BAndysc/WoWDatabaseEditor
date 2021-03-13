namespace WDE.DatabaseEditors.Solution
{
    public class DbTableSolutionItemModifiedField
    {
        public string FieldName { get; }
        public object? NewValue { get; }
        
        public DbTableSolutionItemModifiedField(string fieldName, object? newValue)
        {
            FieldName = fieldName;
            NewValue = newValue;
        }
    }
}