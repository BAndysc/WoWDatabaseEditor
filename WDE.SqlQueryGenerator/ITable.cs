using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    public interface ITable
    {
        DatabaseTable TableName { get; }
        IMultiQuery? CurrentQuery { get; set; }
    }
}