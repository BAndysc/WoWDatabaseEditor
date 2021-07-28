namespace WDE.SqlQueryGenerator
{
    public interface IRow
    {
        T? Column<T>(string name);
        T? Variable<T>(string name);
    }
}