namespace WDE.SqlQueryGenerator
{
    public interface ITable
    {
        string TableName { get; }
        IMultiQuery? CurrentQuery { get; set; }
    }
}