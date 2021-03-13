namespace WDE.DatabaseEditors.Solution
{
    public class DbTableSolutionItemModifiedField
    {
        public string DbFieldName { get; }
        public object? NewValue { get; }
        
        public DbTableSolutionItemModifiedField(string dbFieldName, object? newValue)
        {
            DbFieldName = dbFieldName;
            NewValue = newValue;
        }
    }
}