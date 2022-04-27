namespace WDE.SqlQueryGenerator
{
    public interface IWhere
    {
        ITable Table { get; }
        string Condition { get; }
        bool IsEmpty { get; }
    }
}