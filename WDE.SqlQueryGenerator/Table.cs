using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    internal class Table : ITable
    {
        internal Table(DatabaseTable tableName)
        {
            this.TableName = tableName;
        }

        public DatabaseTable TableName { get; }
        public IMultiQuery? CurrentQuery { get; set; }
    }
}